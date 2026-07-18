import { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { MissionNodesSummary } from './MissionNodesSummary';
import { RankingPanel } from './RankingPanel';
import { colors, typography } from '../../constants/theme';
import type { RankingEntry, TeamFinalSummary } from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';

type FinalSummaryPanelProps = {
  sessionId: string;
  teamId: string;
  sessionStatus: string;
  ranking?: RankingEntry[];
};

function resolveOwnEntry(
  ranking: RankingEntry[],
  teamId: string,
): RankingEntry | null {
  const normalized = teamId.toLowerCase();
  return (
    ranking.find((entry) => entry.teamId.toLowerCase() === normalized) ?? null
  );
}

export function FinalSummaryPanel({
  sessionId,
  teamId,
  sessionStatus,
  ranking = [],
}: FinalSummaryPanelProps) {
  const [summary, setSummary] = useState<TeamFinalSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const isTerminal =
    sessionStatus.toLowerCase() === 'finalized' ||
    sessionStatus.toLowerCase() === 'cancelled';

  const ownEntry = resolveOwnEntry(ranking, teamId);
  const position = summary?.rankingPosition ?? ownEntry?.position ?? null;
  const totalScore = summary?.totalScore ?? ownEntry?.totalScore ?? null;

  useEffect(() => {
    if (!isTerminal) {
      setLoading(false);
      return;
    }

    setLoading(true);
    void gameplayService
      .getTeamFinalSummary(sessionId, teamId)
      .then(setSummary)
      .catch((err) =>
        setError(err instanceof Error ? err.message : 'Could not load summary.'),
      )
      .finally(() => setLoading(false));
  }, [sessionId, teamId, isTerminal]);

  if (!isTerminal) {
    return (
      <Text style={styles.muted}>
        El resumen final se desbloquea cuando el operador termina la sesión.
      </Text>
    );
  }

  if (loading && !ownEntry) {
    return <ActivityIndicator color={colors.primary} />;
  }

  if (summary?.cancellationReason) {
    return (
      <View style={styles.card}>
        <Text style={styles.title}>Sesión cancelada</Text>
        <Text style={styles.reason}>{summary.cancellationReason}</Text>
      </View>
    );
  }

  return (
    <View style={styles.wrapper}>
      <View style={styles.card}>
        <Text style={styles.eyebrow}>DEBRIEF DE MISIÓN</Text>
        <Text style={styles.title}>
          {summary?.teamName ?? ownEntry?.teamName ?? 'Tu equipo'}
        </Text>

        <View style={styles.grid}>
          <Stat
            label="Puntaje total"
            value={totalScore != null ? String(totalScore) : '—'}
          />
          <Stat
            label="Puesto"
            value={position != null ? `#${position}` : '—'}
          />
          <Stat
            label="Nodos"
            value={String(
              summary?.completedNodes ?? ownEntry?.completedNodes ?? '—',
            )}
          />
          <Stat
            label="Sanciones"
            value={String(summary?.penaltiesApplied ?? '—')}
          />
        </View>
        <Text style={styles.meta}>
          Tiempo:{' '}
          {summary?.totalElapsedSeconds?.toFixed(0) ??
            ownEntry?.lastElapsedSeconds.toFixed(0) ??
            '—'}
          s
          {summary?.finalizedAtUtc
            ? ` · Finalizó ${new Date(summary.finalizedAtUtc).toLocaleString()}`
            : ''}
        </Text>
        {error && !summary ? (
          <Text style={styles.fallbackNote}>
            Mostrando datos del ranking en vivo; el resumen oficial no estuvo
            disponible.
          </Text>
        ) : null}
      </View>

      <View style={styles.card}>
        <Text style={styles.sectionEyebrow}>RANKING FINAL</Text>
        <RankingPanel ranking={ranking} />
      </View>

      <View style={styles.card}>
        <Text style={styles.sectionEyebrow}>RECORRIDO</Text>
        <MissionNodesSummary sessionId={sessionId} teamId={teamId} />
      </View>
    </View>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={styles.statValue}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    gap: 16,
  },
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
  sectionEyebrow: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 8,
  },
  title: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '700',
    marginBottom: 16,
  },
  reason: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginBottom: 12,
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
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
  },
  fallbackNote: {
    color: colors.warning,
    fontSize: typography.caption,
    marginTop: 10,
  },
  muted: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
