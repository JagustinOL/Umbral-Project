import { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { TeamFinalSummary } from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';

type FinalSummaryPanelProps = {
  sessionId: string;
  teamId: string;
  sessionStatus: string;
};

export function FinalSummaryPanel({
  sessionId,
  teamId,
  sessionStatus,
}: FinalSummaryPanelProps) {
  const [summary, setSummary] = useState<TeamFinalSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const isTerminal =
    sessionStatus.toLowerCase() === 'finalized' ||
    sessionStatus.toLowerCase() === 'cancelled';

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

  if (loading) {
    return <ActivityIndicator color={colors.primary} />;
  }

  if (error) {
    return <Text style={styles.error}>{error}</Text>;
  }

  if (!summary) {
    return null;
  }

  if (summary.cancellationReason) {
    return (
      <View style={styles.card}>
        <Text style={styles.title}>Sesión cancelada</Text>
        <Text style={styles.reason}>{summary.cancellationReason}</Text>
      </View>
    );
  }

  return (
    <View style={styles.card}>
      <Text style={styles.eyebrow}>DEBRIEF DE MISIÓN</Text>
      <Text style={styles.title}>{summary.teamName ?? 'Tu equipo'}</Text>
      <View style={styles.grid}>
        <Stat label="Puntos" value={String(summary.totalScore ?? 0)} />
        <Stat label="Posición" value={`#${summary.rankingPosition ?? '—'}`} />
        <Stat label="Nodos" value={String(summary.completedNodes ?? 0)} />
        <Stat label="Sanciones" value={String(summary.penaltiesApplied ?? 0)} />
      </View>
      <Text style={styles.meta}>
        Tiempo: {summary.totalElapsedSeconds?.toFixed(0) ?? '—'}s · Finalizó{' '}
        {summary.finalizedAtUtc
          ? new Date(summary.finalizedAtUtc).toLocaleString()
          : '—'}
      </Text>
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
  muted: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
  error: {
    color: colors.danger,
    fontSize: typography.body,
  },
});
