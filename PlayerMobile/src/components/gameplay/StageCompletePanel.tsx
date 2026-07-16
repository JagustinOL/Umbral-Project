import { StyleSheet, Text, View } from 'react-native';
import { PrimaryButton } from '../PrimaryButton';
import { colors, typography } from '../../constants/theme';

export type StageAdvanceSummary = {
  title: string;
  message: string;
  awardedPoints: number;
  completedExecutionOrder: number | null;
  missionCompleted: boolean;
};

type StageCompletePanelProps = {
  summary: StageAdvanceSummary;
  onContinue: () => void;
};

export function StageCompletePanel({
  summary,
  onContinue,
}: StageCompletePanelProps) {
  return (
    <View style={styles.card}>
      <Text style={styles.eyebrow}>
        {summary.missionCompleted ? 'MISIÓN COMPLETADA' : 'ETAPA SUPERADA'}
      </Text>
      <Text style={styles.title}>{summary.title}</Text>
      <Text style={styles.message}>{summary.message}</Text>

      <View style={styles.stats}>
        {summary.completedExecutionOrder != null ? (
          <View style={styles.stat}>
            <Text style={styles.statLabel}>Etapa</Text>
            <Text style={styles.statValue}>
              Nodo #{summary.completedExecutionOrder}
            </Text>
          </View>
        ) : null}
        <View style={styles.stat}>
          <Text style={styles.statLabel}>Puntos ganados</Text>
          <Text style={styles.statValue}>+{summary.awardedPoints}</Text>
        </View>
      </View>

      {summary.missionCompleted ? (
        <Text style={styles.hint}>
          Espera a que el operador finalice la sesión para ver el resumen y el
          ranking finales.
        </Text>
      ) : (
        <PrimaryButton
          label="Continuar a la siguiente etapa"
          onPress={onContinue}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.accent,
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
    marginBottom: 8,
  },
  message: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 16,
  },
  stats: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginBottom: 16,
  },
  stat: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    minWidth: '45%',
    padding: 12,
  },
  statLabel: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 4,
    textTransform: 'uppercase',
  },
  statValue: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
  },
  hint: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
