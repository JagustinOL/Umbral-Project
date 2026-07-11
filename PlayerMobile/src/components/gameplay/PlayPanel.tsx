import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { FormTextField } from '../FormTextField';
import { PrimaryButton } from '../PrimaryButton';
import { GameplayFeedbackBanner } from './GameplayFeedbackBanner';
import { TriviaQuestionPanel } from './TriviaQuestionPanel';
import { colors, typography } from '../../constants/theme';
import type { TeamCurrentNodeContent, TeamCurrentStage } from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';
import { showUserAlert } from '../../utils/confirm';

type PlayPanelProps = {
  sessionId: string;
  teamId: string;
  stage: TeamCurrentStage | null;
  canSubmit: boolean;
  onSubmitted: () => void;
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

      if (result.isCorrect && result.nodeCompleted) {
        setFeedback({
          tone: 'success',
          title: '¡Trivia completada!',
          message: `Etapa superada. +${result.awardedPoints} pts.`,
        });
        showUserAlert(
          '¡Trivia completada!',
          `Etapa superada. +${result.awardedPoints} pts.`,
        );
      } else if (result.isCorrect) {
        setFeedback({
          tone: 'success',
          title: '¡Correcto!',
          message: 'Continúa con la siguiente pregunta.',
        });
        showUserAlert('¡Correcto!', 'Continúa con la siguiente pregunta.');
      } else {
        setFeedback({
          tone: 'error',
          title: 'Incorrecto',
          message: 'Inténtalo de nuevo.',
        });
        showUserAlert('Incorrecto', 'Inténtalo de nuevo.');
      }

      setSelectedOption(null);
      onSubmitted();
      await loadNodeContent();
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
      onSubmitted();
      await loadNodeContent();
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
          message="Todas las etapas fueron superadas. Espera a que el operador finalice la sesión o revisa el ranking."
        />
      </View>
    );
  }

  return (
    <View style={styles.card}>
      <Text style={styles.eyebrow}>ETAPA ACTUAL</Text>
      <Text style={styles.title}>
        Nodo #{stage.currentExecutionOrder ?? '—'}
      </Text>
      <Text style={styles.meta}>
        Tipo: {stage.currentNodeType ?? 'Desconocido'}
      </Text>

      {feedback ? (
        <GameplayFeedbackBanner
          tone={feedback.tone}
          title={feedback.title}
          message={feedback.message}
        />
      ) : null}

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
          <PrimaryButton
            label="Enviar código"
            loading={loading}
            locked={!canSubmit}
            onPress={handleTreasure}
          />
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
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
