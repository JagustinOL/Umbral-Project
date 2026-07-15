import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { FormTextField } from '../FormTextField';
import { PrimaryButton } from '../PrimaryButton';
import { GameplayFeedbackBanner } from './GameplayFeedbackBanner';
import { TriviaQuestionPanel } from './TriviaQuestionPanel';
import { TreasureAreaMap } from './TreasureAreaMap';
import { colors, typography } from '../../constants/theme';
import type { TeamCurrentNodeContent, TeamCurrentStage } from '../../types/gameplay';
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
  onSubmitted: () => void | Promise<unknown>;
  sharedTriviaFeedback?: TriviaFeedback | null;
};

type FeedbackState = {
  tone: 'success' | 'error' | 'info';
  title: string;
  message: string;
} | null;

export function PlayPanel({
  sessionId,
  teamId,
  stage,
  canSubmit,
  onSubmitted,
  sharedTriviaFeedback = null,
}: PlayPanelProps) {
  const [nodeContent, setNodeContent] = useState<TeamCurrentNodeContent | null>(null);
  const [contentLoading, setContentLoading] = useState(false);
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState<FeedbackState>(null);

  const nodeId = stage?.currentNodeId;
  const nodeType = stage?.currentNodeType?.toLowerCase() ?? '';

  const loadNodeContent = useCallback(async () => {
    if (!nodeId || stage?.isCompleted) {
      setNodeContent(null);
      return;
    }

    setContentLoading(true);
    try {
      const content = await gameplayService.getTeamCurrentNodeContent(
        sessionId,
        teamId,
      );
      setNodeContent(content);
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
  }, [nodeId, sessionId, stage?.isCompleted, teamId]);

  useEffect(() => {
    void loadNodeContent();
  }, [loadNodeContent]);

  useEffect(() => {
    if (!sharedTriviaFeedback) {
      return;
    }
    // El banner compartido se muestra a nivel de sesión; aquí solo ocultamos el nodo cerrado.
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
      // Banner compartido llega por SignalR a todo el equipo; alerta local solo al que respondió.
      showUserAlert(nextFeedback.title, nextFeedback.message);

      setSelectedOption(null);
      if (result.nodeCompleted) {
        setNodeContent(null);
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

  const handleTreasure = async () => {
    if (!nodeId || !code.trim()) {
      return;
    }
    setLoading(true);
    try {
      const result = await gameplayService.submitTreasureHuntCode({
        sessionId,
        teamId,
        nodeId,
        foundCode: code,
      });

      if (result.isCorrect) {
        setFeedback({
          tone: 'success',
          title: '¡Tesoro encontrado!',
          message: `Búsqueda completada. +${result.awardedPoints} pts.`,
        });
        showUserAlert(
          '¡Tesoro encontrado!',
          `Búsqueda completada. +${result.awardedPoints} pts.`,
        );
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
      if (result.isCorrect) {
        setNodeContent(null);
      }
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
      setLoading(false);
    }
  };

  if (!stage) {
    return <Text style={styles.muted}>Cargando etapa actual…</Text>;
  }

  if (stage.isCompleted) {
    return (
      <View style={styles.card}>
        <GameplayFeedbackBanner
          tone="success"
          title="¡Misión completada!"
          message="Todas las etapas fueron superadas. Espera a que el operador finalice la sesión; luego podrás ver el resumen y el ranking."
        />
      </View>
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
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Búsqueda del tesoro</Text>
            {nodeContent?.instructions ? (
              <Text style={styles.instructions}>{nodeContent.instructions}</Text>
            ) : null}
            <FormTextField
              label="Código QR / tesoro"
              value={code}
              onChangeText={setCode}
              autoCapitalize="characters"
              editable={canSubmit}
            />
            {nodeContent?.destination ? (
              <TreasureAreaMap
                latitude={nodeContent.destination.latitude}
                longitude={nodeContent.destination.longitude}
              />
            ) : null}
            <PrimaryButton
              label="Enviar código"
              loading={loading}
              locked={!canSubmit}
              onPress={handleTreasure}
            />
          </View>
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
  section: {
    marginTop: 16,
  },
  sectionTitle: {
    color: colors.text,
    fontSize: typography.body,
    fontWeight: '600',
    marginBottom: 8,
  },
  instructions: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 12,
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
