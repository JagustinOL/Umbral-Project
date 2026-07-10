import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { TeamHint } from '../../types/gameplay';

type HintsPanelProps = {
  hints: TeamHint[];
};

export function HintsPanel({ hints }: HintsPanelProps) {
  if (hints.length === 0) {
    return (
      <View style={styles.empty}>
        <Text style={styles.emptyTitle}>Sin pistas aún</Text>
        <Text style={styles.emptyText}>
          Las pistas liberadas aparecerán aquí en tiempo real.
        </Text>
      </View>
    );
  }

  return (
    <View style={styles.list}>
      {hints.map((hint) => (
        <View key={`${hint.hintId}-${hint.releasedAtUtc}`} style={styles.card}>
          <Text style={styles.badge}>
            {hint.wasManualRelease ? 'OPERATOR' : 'AUTO'} · −{hint.penaltyPoints} pts
          </Text>
          <Text style={styles.content}>{hint.content}</Text>
          <Text style={styles.meta}>
            Node {hint.missionNodeId.slice(0, 8)}… ·{' '}
            {new Date(hint.releasedAtUtc).toLocaleTimeString()}
          </Text>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  list: {
    gap: 12,
  },
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    padding: 14,
  },
  badge: {
    color: colors.warning,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 0.6,
    marginBottom: 8,
  },
  content: {
    color: colors.text,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 8,
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
