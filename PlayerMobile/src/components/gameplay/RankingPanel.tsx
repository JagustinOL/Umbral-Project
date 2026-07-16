import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { RankingEntry } from '../../types/gameplay';
import type { RankingEvent } from '../../utils/rankingEvents';
import { useAuth } from '../../hooks/useAuth';

type RankingPanelProps = {
  ranking: RankingEntry[];
  events?: RankingEvent[];
};

const eventToneColor = {
  up: colors.accent,
  down: colors.danger,
  same: colors.textMuted,
  info: colors.text,
} as const;

export function RankingPanel({ ranking, events = [] }: RankingPanelProps) {
  const { session } = useAuth();
  const teamId = session?.teamId?.toLowerCase();

  return (
    <View style={styles.wrapper}>
      {events.length > 0 ? (
        <View style={styles.eventsCard}>
          <Text style={styles.eventsTitle}>Eventos del ranking</Text>
          {events.slice(0, 6).map((event) => (
            <Text
              key={event.id}
              style={[styles.eventLine, { color: eventToneColor[event.tone] }]}
            >
              {event.message}
            </Text>
          ))}
        </View>
      ) : null}

      {ranking.length === 0 ? (
        <Text style={styles.empty}>
          Ranking se actualizará cuando los equipos sumen puntos.
        </Text>
      ) : (
        <View style={styles.list}>
          {ranking.map((entry) => {
            const isYou = entry.teamId.toLowerCase() === teamId;
            return (
              <View
                key={entry.teamId}
                style={[styles.row, isYou ? styles.rowHighlight : undefined]}
              >
                <Text style={styles.position}>#{entry.position}</Text>
                <View style={styles.info}>
                  <Text style={styles.name}>
                    {entry.teamName}
                    {isYou ? ' (tú)' : ''}
                  </Text>
                  <Text style={styles.meta}>
                    {entry.completedNodes} nodes · {entry.lastElapsedSeconds.toFixed(0)}s
                  </Text>
                </View>
                <Text style={styles.score}>{entry.totalScore}</Text>
              </View>
            );
          })}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    gap: 16,
  },
  eventsCard: {
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    gap: 8,
    padding: 12,
  },
  eventsTitle: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 0.5,
    marginBottom: 2,
    textTransform: 'uppercase',
  },
  eventLine: {
    fontSize: typography.body,
    lineHeight: 20,
  },
  list: {
    gap: 8,
  },
  row: {
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    flexDirection: 'row',
    padding: 12,
  },
  rowHighlight: {
    borderColor: colors.accent,
  },
  position: {
    color: colors.accent,
    fontSize: typography.subtitle,
    fontWeight: '800',
    width: 36,
  },
  info: {
    flex: 1,
  },
  name: {
    color: colors.text,
    fontSize: typography.body,
    fontWeight: '600',
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginTop: 2,
  },
  score: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
  },
  empty: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
