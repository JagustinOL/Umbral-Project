import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { TeamPenalty } from '../../types/gameplay';

type PenaltiesPanelProps = {
  penalties: TeamPenalty[];
  lastAlert?: string | null;
};

export function PenaltiesPanel({ penalties, lastAlert }: PenaltiesPanelProps) {
  return (
    <View>
      {lastAlert ? (
        <View style={styles.alert}>
          <Text style={styles.alertText}>{lastAlert}</Text>
        </View>
      ) : null}

      {penalties.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyTitle}>Sin sanciones</Text>
          <Text style={styles.emptyText}>
            No hay penalizaciones aplicadas a tu equipo.
          </Text>
        </View>
      ) : (
        <View style={styles.list}>
          {penalties.map((penalty) => (
            <View key={penalty.entryId} style={styles.card}>
              <Text style={styles.points}>−{penalty.penaltyPoints} pts</Text>
              <Text style={styles.reason}>{penalty.reason}</Text>
              <Text style={styles.meta}>
                {penalty.category} ·{' '}
                {new Date(penalty.appliedAtUtc).toLocaleString()}
              </Text>
            </View>
          ))}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  alert: {
    backgroundColor: '#3d1f1f',
    borderRadius: 8,
    marginBottom: 12,
    padding: 12,
  },
  alertText: {
    color: colors.danger,
    fontSize: typography.body,
    lineHeight: 20,
  },
  list: {
    gap: 10,
  },
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    padding: 14,
  },
  points: {
    color: colors.danger,
    fontSize: typography.subtitle,
    fontWeight: '700',
    marginBottom: 4,
  },
  reason: {
    color: colors.text,
    fontSize: typography.body,
    lineHeight: 20,
    marginBottom: 6,
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
  },
  empty: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderStyle: 'dashed',
    borderWidth: 1,
    padding: 20,
  },
  emptyTitle: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '600',
    marginBottom: 6,
  },
  emptyText: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
