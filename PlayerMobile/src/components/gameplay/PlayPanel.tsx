import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { GameplayFeedbackBanner } from './GameplayFeedbackBanner';
import {
  StageCompletePanel,
  type StageAdvanceSummary,
} from './StageCompletePanel';
import { TriviaQuestionPanel } from './TriviaQuestionPanel';
import { TreasureHuntPanel } from './TreasureHuntPanel';
import { colors, typography } from '../../constants/theme';
import type {
  RankingEntry,
  TeamCurrentNodeContent,
  TeamCurrentStage,
} from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';
import { showUserAlert } from '../../utils/confirm';
import {
  feedbackFromTriviaResult,
  type TriviaFeedback,
} from '../../utils/triviaFeedback';

const ERROR_FEEDBACK_MS = 5000;

type PlayPanelProps = {
  sessionId: string;
  teamId: string;
  stage: TeamCurrentStage | null;
  canSubmit: boolean;
  ranking?: RankingEntry[];
  onSubmitted: () => void | Promise<unknown>;
  sharedTriviaFeedback?: TriviaFeedback | null;
  stageAdvance?: StageAdvanceSummary | null;
  onConfirmStageAdvance?: () => void;
  onStageCompleted?: (summary: {
    awardedPoints: number;
    title: string;
    message: string;
  }) => void;
};

type FeedbackState = {
  tone: 'success' | 'error' | 'info';
  title: string;
  message: string;
} | null;

/** Normaliza el payload por si el gateway/serialización usa PascalCase. */
function normalizeNodeContent(raw: TeamCurrentNodeContent): TeamCurrentNodeContent {
  const anyRaw = raw as TeamCurrentNodeContent & {
    Instructions?: string | null;
    Destination?: { Latitude?: number; Longitude?: number } | null;
  };

  const instructions =
    (typeof raw.instructions === 'string' && raw.instructions.trim()) ||
    (typeof anyRaw.Instructions === 'string' && anyRaw.Instructions.trim()) ||
    null;

  const destination =
    raw.destination ??
    (anyRaw.Destination &&
    typeof anyRaw.Destination.Latitude === 'number' &&
    typeof anyRaw.Destination.Longitude === 'number'
      ? {
          latitude: anyRaw.Destination.Latitude,
          longitude: anyRaw.Destination.Longitude,
        }
      : null);

  return {
    ...raw,
    instructions,
    destination,
  };
}

export function PlayPanel({
  sessionId,
  teamId,
  stage,
  canSubmit,
  ranking = [],
  onSubmitted,
  sharedTriviaFeedback = null,
  stageAdvance = null,
  onConfirmStageAdvance,
  onStageCompleted,
}: PlayPanelProps) {
  const [nodeContent, setNodeContent] = useState<TeamCurrentNodeContent | null>(null);
  const [contentLoading, setContentLoading] = useState(false);
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState<FeedbackState>(null);
  const [scannerOpen, setScannerOpen] = useState(false);
  const submitLockRef = useRef(false);

  const nodeId = stage?.currentNodeId;
  const nodeType = stage?.currentNodeType?.toLowerCase() ?? '';

  const loadNodeContent = useCallback(async () => {
    if (!nodeId || stage?.isCompleted || stageAdvance) {
      setNodeContent(null);
      return;
    }

    setContentLoading(true);
    try {
      const content = await gameplayService.getTeamCurrentNodeContent(
        sessionId,
        teamId,
      );
      setNodeContent(normalizeNodeContent(content));
      setSelectedOption(null);
    } catch (error) {
      setNodeContent(null);
      showUserAlert(
        'No se pudo cargar la etapa',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setContentLoading(false);
    }
  }, [nodeId, sessionId, stage?.isCompleted, stageAdvance, teamId]);

  useEffect(() => {
    void loadNodeContent();
  }, [loadNodeContent]);

  useEffect(() => {
    if (!sharedTriviaFeedback) {
      return;
    }
    if (sharedTriviaFeedback.nodeCompleted) {
      setNodeContent(null);
    }
  }, [sharedTriviaFeedback]);

  useEffect(() => {
    if (feedback?.tone !== 'error') {
      return;
    }

    const timer = setTimeout(() => {
      setFeedback(null);
    }, ERROR_FEEDBACK_MS);

    return () => clearTimeout(timer);
  }, [feedback]);

  const currentQuestionIndex =
    nodeContent?.currentQuestionIndex ?? 0;
  const currentQuestion =
    nodeContent?.questions?.[currentQuestionIndex] ?? null;

  const handleTrivia = async () => {
    if (!nodeId || !selectedOption) {
      return;
    }

    setLoading(true);
    try {
      const result = await gameplayService.submitTriviaAnswer({
        sessionId,
        teamId,
        nodeId,
        answer: selectedOption,
        questionIndex: currentQuestionIndex,
      });

      const nextFeedback = feedbackFromTriviaResult(result);
      showUserAlert(nextFeedback.title, nextFeedback.message);

      setSelectedOption(null);
      if (result.nodeCompleted) {
        setNodeContent(null);
        onStageCompleted?.({
          awardedPoints: result.awardedPoints,
          title: nextFeedback.title,
          message: nextFeedback.message,
        });
      }
      await onSubmitted();
      if (!result.nodeCompleted) {
        await loadNodeContent();
      }
    } catch (error) {
      showUserAlert(
        'Error al enviar',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setLoading(false);
    }
  };

  const handleTreasure = async (rawCode?: string) => {
    const foundCode = (rawCode ?? code).trim();
    if (!nodeId || !foundCode || submitLockRef.current) {
      return;
    }

    submitLockRef.current = true;
    setLoading(true);
    setScannerOpen(false);
    try {
      const result = await gameplayService.submitTreasureHuntCode({
        sessionId,
        teamId,
        nodeId,
        foundCode,
      });

      if (result.isCorrect) {
        const title = '¡Tesoro encontrado!';
        const message = `Búsqueda completada. +${result.awardedPoints} pts.`;
        setFeedback({ tone: 'success', title, message });
        setNodeContent(null);
        onStageCompleted?.({
          awardedPoints: result.awardedPoints,
          title,
          message,
        });
      } else {
        setFeedback({
          tone: 'error',
          title: 'Código inválido',
          message: 'Escanea o ingresa el código QR correcto.',
        });
        showUserAlert(
          'Código inválido',
          'Escanea o ingresa el código QR correcto.',
        );
      }

      setCode('');
      await onSubmitted();
      if (!result.isCorrect) {
        await loadNodeContent();
      }
    } catch (error) {
      showUserAlert(
        'Error al enviar código',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      submitLockRef.current = false;
      setLoading(false);
    }
  };

  const handleQrScanned = (value: string) => {
    if (!canSubmit || submitLockRef.current) {
      return;
    }
    setCode(value.trim());
    void handleTreasure(value);
  };

  const handleContinue = () => {
    onConfirmStageAdvance?.();
  };

  if (!stage) {
    return <Text style={styles.muted}>Cargando etapa actual…</Text>;
  }

  if (stageAdvance) {
    return (
      <StageCompletePanel
        summary={stageAdvance}
        onContinue={handleContinue}
        ranking={ranking}
        teamId={teamId}
        sessionId={sessionId}
      />
    );
  }

  if (stage.isCompleted) {
    return (
      <StageCompletePanel
        summary={{
          title: '¡Misión completada!',
          message: 'Todas las etapas fueron superadas.',
          awardedPoints: 0,
          completedExecutionOrder: null,
          missionCompleted: true,
        }}
        onContinue={handleContinue}
        ranking={ranking}
        teamId={teamId}
        sessionId={sessionId}
      />
    );
  }

  return (
    <View style={styles.wrapper}>
      {feedback ? (
        <GameplayFeedbackBanner
          tone={feedback.tone}
          title={feedback.title}
          message={feedback.message}
        />
      ) : null}

      <View style={styles.card}>
        <Text style={styles.eyebrow}>ETAPA ACTUAL</Text>
        <Text style={styles.title}>
          Nodo #{stage.currentExecutionOrder ?? '—'}
        </Text>
        <Text style={styles.meta}>
          Tipo: {stage.currentNodeType ?? 'Desconocido'}
        </Text>

        {!canSubmit ? (
          <Text style={styles.blocked}>
            Los envíos están deshabilitados mientras la sesión está pausada o finalizada.
          </Text>
        ) : null}

        {contentLoading ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : null}

        {nodeType.includes('trivia') && currentQuestion && nodeContent ? (
          <TriviaQuestionPanel
            question={currentQuestion}
            questionIndex={currentQuestionIndex}
            totalQuestions={nodeContent.totalQuestions}
            selectedOption={selectedOption}
            loading={loading}
            canSubmit={canSubmit}
            onSelectOption={setSelectedOption}
            onSubmit={handleTrivia}
          />
        ) : null}

        {nodeType.includes('treasure') ? (
          <TreasureHuntPanel
            instructions={nodeContent?.instructions ?? null}
            contentLoading={contentLoading}
            code={code}
            onChangeCode={setCode}
            canSubmit={canSubmit}
            loading={loading}
            scannerOpen={scannerOpen}
            onOpenScanner={() => setScannerOpen(true)}
            onCloseScanner={() => setScannerOpen(false)}
            onScanned={handleQrScanned}
            onSubmit={() => void handleTreasure()}
            destination={nodeContent?.destination ?? null}
          />
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    gap: 0,
  },
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 12,
    borderWidth: 1,
    padding: 16,
  },
  eyebrow: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 6,
  },
  title: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
    marginBottom: 4,
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 16,
  },
  muted: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
  blocked: {
    color: colors.warning,
    fontSize: typography.body,
    lineHeight: 20,
    marginBottom: 12,
  },
});
