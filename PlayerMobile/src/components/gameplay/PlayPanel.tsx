import { useState } from 'react';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { FormTextField } from '../FormTextField';
import { PrimaryButton } from '../PrimaryButton';
import { colors, typography } from '../../constants/theme';
import type { TeamCurrentStage } from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';

type PlayPanelProps = {
  sessionId: string;
  teamId: string;
  stage: TeamCurrentStage | null;
  canSubmit: boolean;
  onSubmitted: () => void;
};

export function PlayPanel({
  sessionId,
  teamId,
  stage,
  canSubmit,
  onSubmitted,
}: PlayPanelProps) {
  const [answer, setAnswer] = useState('');
  const [code, setCode] = useState('');
  const [evidence, setEvidence] = useState('');
  const [loading, setLoading] = useState(false);

  const nodeId = stage?.currentNodeId;
  const nodeType = stage?.currentNodeType?.toLowerCase() ?? '';

  const handleTrivia = async () => {
    if (!nodeId || !answer.trim()) {
      return;
    }
    setLoading(true);
    try {
      const result = await gameplayService.submitTriviaAnswer({
        sessionId,
        teamId,
        nodeId,
        answer,
      });
      Alert.alert(
        result.isCorrect ? '¡Correcto!' : 'Incorrecto',
        result.isCorrect
          ? `+${result.awardedPoints} pts. Etapa superada.`
          : 'Inténtalo de nuevo.',
      );
      setAnswer('');
      onSubmitted();
    } catch (error) {
      Alert.alert(
        'Submission failed',
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
      Alert.alert(
        result.isCorrect ? '¡Código válido!' : 'Código inválido',
        result.isCorrect
          ? `Tesoro encontrado. +${result.awardedPoints} pts.`
          : 'Escanea o ingresa el código QR correcto.',
      );
      setCode('');
      onSubmitted();
    } catch (error) {
      Alert.alert(
        'Code rejected',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setLoading(false);
    }
  };

  const handleEvidence = async () => {
    if (!nodeId || !evidence.trim()) {
      return;
    }
    setLoading(true);
    try {
      const result = await gameplayService.submitEvidence({
        sessionId,
        teamId,
        nodeId,
        content: evidence,
      });
      Alert.alert(
        'Evidencia enviada',
        `Estado: ${result.status}. Esperando revisión del operador.`,
      );
      setEvidence('');
      onSubmitted();
    } catch (error) {
      Alert.alert(
        'Upload failed',
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
        <Text style={styles.title}>Todas las etapas completadas</Text>
        <Text style={styles.muted}>
          Espera a que el operador finalice la sesión o revisa el ranking.
        </Text>
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
        Tipo: {stage.currentNodeType ?? 'Desconocido'} · ID {nodeId?.slice(0, 8)}…
      </Text>

      {!canSubmit ? (
        <Text style={styles.blocked}>
          Los envíos están deshabilitados mientras la sesión está pausada o finalizada.
        </Text>
      ) : null}

      {nodeType.includes('trivia') ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Respuesta de trivia</Text>
          <FormTextField
            label="Tu respuesta"
            value={answer}
            onChangeText={setAnswer}
            editable={canSubmit}
          />
          <PrimaryButton
            label="Enviar respuesta"
            loading={loading}
            locked={!canSubmit}
            onPress={handleTrivia}
          />
        </View>
      ) : null}

      {nodeType.includes('treasure') ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Código de búsqueda del tesoro</Text>
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

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Evidencia manual</Text>
        <FormTextField
          label="Descripción o notas"
          value={evidence}
          onChangeText={setEvidence}
          multiline
          editable={canSubmit}
        />
        <PrimaryButton
          label="Enviar evidencia para revisión"
          variant="ghost"
          loading={loading}
          locked={!canSubmit}
          onPress={handleEvidence}
        />
      </View>
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
