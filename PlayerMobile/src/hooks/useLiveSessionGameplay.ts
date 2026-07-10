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
} from '../services/signalRService';

type UseLiveSessionGameplayOptions = {
  sessionId: string;
  teamId: string;
  enabled?: boolean;
};

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
  const [hints, setHints] = useState<TeamHint[]>([]);
  const [penalties, setPenalties] = useState<TeamPenalty[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [lastPenaltyAlert, setLastPenaltyAlert] = useState<string | null>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const refreshStage = useCallback(async () => {
    const next = await gameplayService.getTeamCurrentStage(sessionId, teamId);
    setStage(next);
    return next;
  }, [sessionId, teamId]);

  const refreshRanking = useCallback(async () => {
    const next = await gameplayService.getSessionRanking(sessionId);
    setRanking(next);
    return next;
  }, [sessionId]);

  const refreshHints = useCallback(async () => {
    const next = await gameplayService.getTeamHints(sessionId, teamId);
    setHints(next);
    if (next.length > hints.length) {
      void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
    }
    return next;
  }, [sessionId, teamId, hints.length]);

  const refreshPenalties = useCallback(async () => {
    const next = await gameplayService.getTeamPenalties(sessionId, teamId);
    setPenalties(next);
    return next;
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
        `Penalty −${payload.penaltyPoints}: ${payload.reason}`,
      );
      void refreshPenalties();
      void refreshRanking();
    },
    [teamId, refreshPenalties, refreshRanking],
  );

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
        setRanking(payload.ranking ?? []);
      },
      onManualPenalty: handlePenalty,
      onReconnecting: () => setConnectionState('reconnecting'),
      onReconnected: () => {
        setConnectionState('connected');
        void refreshAll();
      },
    })
      .then(() => setConnectionState('connected'))
      .catch(() => setConnectionState('disconnected'));

    pollRef.current = setInterval(() => {
      void refreshStage();
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
    syncSessionStatus,
    handlePenalty,
  ]);

  return {
    sessionStatus,
    connectionState,
    stage,
    ranking,
    hints,
    penalties,
    isLoading,
    lastPenaltyAlert,
    isPlayable: isSessionPlayable(sessionStatus),
    isPaused: sessionStatus.toLowerCase() === 'paused',
    isTerminal: isSessionTerminal(sessionStatus),
    refreshAll,
    refreshStage,
    refreshRanking,
    refreshHints,
    refreshPenalties,
    clearPenaltyAlert: () => setLastPenaltyAlert(null),
  };
}
