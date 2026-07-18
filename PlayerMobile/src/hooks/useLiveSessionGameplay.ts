import * as Haptics from 'expo-haptics';
import { useCallback, useEffect, useRef, useState } from 'react';
import type {
  LiveSessionStatus,
  RankingEntry,
  TeamCurrentStage,
  TeamHint,
  TeamPenalty,
} from '../types/gameplay';
import {
  isSessionPlayable,
  isSessionTerminal,
} from '../types/gameplay';
import * as gameplayService from '../services/gameplayService';
import * as liveSessionService from '../services/liveSessionService';
import {
  connectLiveSessionHub,
  disconnectLiveSessionHub,
  type ManualPenaltyPayload,
  type SupportMessagePayload,
  type TriviaAnswerSubmittedPayload,
} from '../services/signalRService';
import type { StageAdvanceSummary } from '../components/gameplay/StageCompletePanel';
import { feedbackFromTriviaResult, type TriviaFeedback } from '../utils/triviaFeedback';
import {
  buildRankingEvents,
  type RankingEvent,
} from '../utils/rankingEvents';

type UseLiveSessionGameplayOptions = {
  sessionId: string;
  teamId: string;
  enabled?: boolean;
};

const MAX_RANKING_EVENTS = 12;

export function useLiveSessionGameplay({
  sessionId,
  teamId,
  enabled = true,
}: UseLiveSessionGameplayOptions) {
  const [sessionStatus, setSessionStatus] = useState<LiveSessionStatus>('Active');
  const [connectionState, setConnectionState] = useState<
    'connected' | 'reconnecting' | 'disconnected'
  >('disconnected');
  const [stage, setStage] = useState<TeamCurrentStage | null>(null);
  const [ranking, setRanking] = useState<RankingEntry[]>([]);
  const [rankingEvents, setRankingEvents] = useState<RankingEvent[]>([]);
  const [hints, setHints] = useState<TeamHint[]>([]);
  const [penalties, setPenalties] = useState<TeamPenalty[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [lastPenaltyAlert, setLastPenaltyAlert] = useState<string | null>(null);
  const [supportMessage, setSupportMessage] = useState<string | null>(null);
  const [triviaFeedback, setTriviaFeedback] = useState<TriviaFeedback | null>(null);
  const [stageAdvance, setStageAdvance] = useState<StageAdvanceSummary | null>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const rankingRef = useRef<RankingEntry[]>([]);
  const stageRef = useRef<TeamCurrentStage | null>(null);

  const applyRanking = useCallback(
    (next: RankingEntry[]) => {
      const previous = rankingRef.current;
      rankingRef.current = next;
      setRanking(next);

      // Primera carga: no generar ruido de "entra al ranking".
      if (previous.length === 0) {
        return;
      }

      const events = buildRankingEvents(previous, next, teamId);
      if (events.length > 0) {
        setRankingEvents((prev) => [...events, ...prev].slice(0, MAX_RANKING_EVENTS));
        const ownMoved = events.some(
          (event) =>
            event.teamId.toLowerCase() === teamId.toLowerCase() &&
            (event.tone === 'up' || event.tone === 'down'),
        );
        if (ownMoved) {
          void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
        }
      }
    },
    [teamId],
  );

  const refreshStage = useCallback(async () => {
    const next = await gameplayService.getTeamCurrentStage(sessionId, teamId);
    stageRef.current = next;
    setStage(next);
    return next;
  }, [sessionId, teamId]);

  const refreshRanking = useCallback(async () => {
    const next = await gameplayService.getSessionRanking(sessionId);
    applyRanking(next);
    return next;
  }, [sessionId, applyRanking]);

  const refreshHints = useCallback(async () => {
    const next = await gameplayService.getTeamHints(sessionId, teamId);
    setHints(next);
    if (next.length > hints.length) {
      void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
    }
    return next;
  }, [sessionId, teamId, hints.length]);

  const refreshPenalties = useCallback(async () => {
    try {
      const next = await gameplayService.getTeamPenalties(sessionId, teamId);
      setPenalties(next);
      return next;
    } catch (error) {
      if (__DEV__) {
        console.warn('Failed to refresh penalties:', error);
      }
      // No vaciar la lista optimista si el API falla.
      return null;
    }
  }, [sessionId, teamId]);

  const syncSessionStatus = useCallback(async () => {
    const sessions = await liveSessionService.getActiveSessions();
    const match = sessions.find(
      (s) => s.sessionId.toLowerCase() === sessionId.toLowerCase(),
    );
    if (match) {
      setSessionStatus(match.status);
      return match.status;
    }
    return sessionStatus;
  }, [sessionId, sessionStatus]);

  const refreshAll = useCallback(async () => {
    setIsLoading(true);
    try {
      const results = await Promise.allSettled([
        refreshStage(),
        refreshRanking(),
        refreshHints(),
        refreshPenalties(),
        syncSessionStatus(),
      ]);

      const failed = results.filter((r) => r.status === 'rejected');
      if (failed.length > 0 && __DEV__) {
        console.warn(
          'Live session refresh partial failure:',
          failed.map((r) => (r.status === 'rejected' ? r.reason : null)),
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [
    refreshStage,
    refreshRanking,
    refreshHints,
    refreshPenalties,
    syncSessionStatus,
  ]);

  const handlePenalty = useCallback(
    (payload: ManualPenaltyPayload) => {
      if (payload.teamId.toLowerCase() !== teamId.toLowerCase()) {
        return;
      }
      void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
      setLastPenaltyAlert(
        `Sanción −${payload.penaltyPoints} pts: ${payload.reason}`,
      );
      // Optimistic: ScoringAudit aún no persistió cuando llega ReceiveManualPenalty.
      // El refresh real ocurre en onScoreUpdate (ledger ya actualizado).
      setPenalties((prev) => [
        {
          entryId: `live-manual-${payload.penaltyPoints}-${Date.now()}`,
          penaltyPoints: payload.penaltyPoints,
          reason: payload.reason,
          category: 'ManualOperator',
          appliedAtUtc: new Date().toISOString(),
        },
        ...prev,
      ]);
      void refreshRanking();
    },
    [teamId, refreshRanking],
  );

  const handleSupportMessage = useCallback((payload: SupportMessagePayload) => {
    if (payload.teamId.toLowerCase() !== teamId.toLowerCase()) {
      return;
    }
    void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
    setSupportMessage(payload.message);
  }, [teamId]);

  const openStageAdvance = useCallback(
    (input: {
      awardedPoints: number;
      title?: string;
      message?: string;
      missionCompleted?: boolean;
    }) => {
      const previousOrder = stageRef.current?.currentExecutionOrder ?? null;
      setStageAdvance((prev) => {
        // Si ya hay resumen con puntos (trivia), no lo pisar con un evento genérico de progreso.
        if (prev && prev.awardedPoints > 0 && input.awardedPoints === 0) {
          return {
            ...prev,
            missionCompleted: Boolean(input.missionCompleted) || prev.missionCompleted,
          };
        }

        return {
          title: input.title ?? '¡Etapa completada!',
          message:
            input.message ??
            (input.missionCompleted
              ? 'Completaste todas las etapas de la misión.'
              : 'Revisa el resumen y continúa cuando estés listo.'),
          awardedPoints: input.awardedPoints,
          completedExecutionOrder: prev?.completedExecutionOrder ?? previousOrder,
          missionCompleted: Boolean(input.missionCompleted),
        };
      });
    },
    [],
  );

  const handleTriviaAnswerSubmitted = useCallback(
    (payload: TriviaAnswerSubmittedPayload) => {
      if (payload.teamId.toLowerCase() !== teamId.toLowerCase()) {
        return;
      }

      const feedback = feedbackFromTriviaResult({
        isCorrect: payload.isCorrect,
        nodeCompleted: payload.nodeCompleted,
        awardedPoints: payload.awardedPoints ?? 0,
      });
      setTriviaFeedback(feedback);

      if (payload.nodeCompleted) {
        openStageAdvance({
          awardedPoints: payload.awardedPoints ?? 0,
          title: feedback.title,
          message: feedback.message,
        });
      }

      if (payload.isCorrect) {
        void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
      } else {
        void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
      }
    },
    [teamId, openStageAdvance],
  );

  useEffect(() => {
    if (triviaFeedback?.tone !== 'error') {
      return;
    }

    const timer = setTimeout(() => {
      setTriviaFeedback(null);
    }, 5000);

    return () => clearTimeout(timer);
  }, [triviaFeedback]);

  useEffect(() => {
    if (!enabled || !sessionId || !teamId) {
      return;
    }

    void refreshAll();

    void connectLiveSessionHub(sessionId, {
      onSessionStateChanged: (payload) => {
        setSessionStatus(payload.newStatus);
        if (payload.newStatus.toLowerCase() === 'paused') {
          void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
        }
      },
      onScoreUpdate: (payload) => {
        applyRanking(payload.ranking ?? []);
        // Tras penalización/pista, el ledger ya está en ScoringAudit.
        if (payload.teamId.toLowerCase() === teamId.toLowerCase()) {
          void refreshPenalties();
        }
      },
      onManualPenalty: handlePenalty,
      onHintReleased: (payload) => {
        if (payload.teamId.toLowerCase() !== teamId.toLowerCase()) {
          return;
        }
        void refreshHints();
        if (payload.penaltyPoints > 0) {
          setPenalties((prev) => {
            const entryId = `live-hint-${payload.hintId}`;
            const withoutDup = prev.filter((p) => p.entryId !== entryId);
            return [
              {
                entryId,
                penaltyPoints: payload.penaltyPoints,
                reason: 'Penalización por uso de pista.',
                category: 'HintPenalty',
                appliedAtUtc: new Date().toISOString(),
              },
              ...withoutDup,
            ];
          });
        }
      },
      onSupportMessage: handleSupportMessage,
      onTriviaAnswerSubmitted: handleTriviaAnswerSubmitted,
      onTeamProgressUpdated: (payload) => {
        if (payload.teamId.toLowerCase() !== teamId.toLowerCase()) {
          return;
        }
        if (payload.nodeCompleted) {
          // Trivia ya abre el resumen vía TriviaAnswerSubmitted; solo cubrir tesoro/otros.
          setStageAdvance((prev) => {
            if (prev) {
              return payload.nextNodeId
                ? prev
                : { ...prev, missionCompleted: true };
            }
            return {
              title: '¡Etapa superada!',
              message: payload.nextNodeId
                ? 'Tu equipo avanzó. Continúa cuando estés listo para la siguiente etapa.'
                : 'Tu equipo completó todas las etapas.',
              awardedPoints: 0,
              completedExecutionOrder: stageRef.current?.currentExecutionOrder ?? null,
              missionCompleted: !payload.nextNodeId,
            };
          });
          void refreshRanking();
        }
        void refreshStage().then((next) => {
          if (payload.nodeCompleted && next.isCompleted) {
            setStageAdvance((prev) =>
              prev ? { ...prev, missionCompleted: true } : prev,
            );
          }
        });
      },
      onReconnecting: () => setConnectionState('reconnecting'),
      onReconnected: () => {
        setConnectionState('connected');
        void refreshAll();
      },
    }, teamId)
      .then(() => setConnectionState('connected'))
      .catch(() => setConnectionState('disconnected'));

    pollRef.current = setInterval(() => {
      void refreshStage();
      void refreshRanking();
      void refreshPenalties();
      void syncSessionStatus();
    }, 15000);

    return () => {
      if (pollRef.current) {
        clearInterval(pollRef.current);
      }
      void disconnectLiveSessionHub();
    };
  }, [
    enabled,
    sessionId,
    teamId,
    refreshAll,
    refreshStage,
    refreshRanking,
    refreshPenalties,
    syncSessionStatus,
    handlePenalty,
    handleSupportMessage,
    handleTriviaAnswerSubmitted,
    refreshHints,
    applyRanking,
    openStageAdvance,
  ]);

  return {
    sessionStatus,
    connectionState,
    stage,
    ranking,
    rankingEvents,
    stageAdvance,
    hints,
    penalties,
    isLoading,
    lastPenaltyAlert,
    supportMessage,
    triviaFeedback,
    isPlayable: isSessionPlayable(sessionStatus),
    isPaused: sessionStatus.toLowerCase() === 'paused',
    isTerminal: isSessionTerminal(sessionStatus),
    refreshAll,
    refreshStage,
    refreshRanking,
    refreshHints,
    refreshPenalties,
    openStageAdvance,
    confirmStageAdvance: () => {
      setStageAdvance(null);
      setTriviaFeedback(null);
    },
    clearPenaltyAlert: () => setLastPenaltyAlert(null),
    clearSupportMessage: () => setSupportMessage(null),
    clearTriviaFeedback: () => setTriviaFeedback(null),
  };
}
