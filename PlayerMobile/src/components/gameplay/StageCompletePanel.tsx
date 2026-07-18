import { StyleSheet, Text, View } from 'react-native';
import { PrimaryButton } from '../PrimaryButton';
import { colors, typography } from '../../constants/theme';
import type { RankingEntry } from '../../types/gameplay';

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
  ranking?: RankingEntry[];
  teamId?: string;
  sessionId?: string;
};

function resolveOwnEntry(
  ranking: RankingEntry[],
  teamId?: string,
): RankingEntry | null {
  if (!teamId) {
    return null;
  }
  const normalized = teamId.toLowerCase();
  return (
    ranking.find((entry) => entry.teamId.toLowerCase() === normalized) ?? null
  );
}

export function StageCompletePanel({
  summary,
  onContinue,
  ranking = [],
  teamId,
}: StageCompletePanelProps) {
  const ownEntry = resolveOwnEntry(ranking, teamId);
  const position = ownEntry?.position ?? null;

  if (summary.missionCompleted) {
    return (
      <View style={styles.card}>
        <Text style={styles.eyebrow}>MISIÓN COMPLETADA</Text>
        <Text style={styles.title}>Tu equipo terminó la misión</Text>
        <View style={styles.stats}>
          <View style={styles.stat}>
            <Text style={styles.statLabel}>Puntaje total</Text>
            <Text style={styles.statValue}>
              {ownEntry != null ? ownEntry.totalScore : '—'}
            </Text>
          </View>
          <View style={styles.stat}>
            <Text style={styles.statLabel}>Puesto actual</Text>
            <Text style={styles.statValue}>
              {position != null ? `#${position}` : '—'}
            </Text>
          </View>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.card}>
      <Text style={styles.eyebrow}>ETAPA SUPERADA</Text>
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

      <PrimaryButton
        label="Continuar a la siguiente etapa"
        onPress={onContinue}
      />
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
    marginBottom: 16,
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
  },
  stat: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    minWidth: '45%',
    flex: 1,
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
});
