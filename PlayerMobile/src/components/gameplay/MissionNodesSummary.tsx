import { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { MissionProgressNode, TeamMissionProgress } from '../../types/gameplay';
import * as gameplayService from '../../services/gameplayService';

type MissionNodesSummaryProps = {
  sessionId: string;
  teamId: string;
};

function nodeTypeLabel(nodeType: string): string {
  const normalized = nodeType.toLowerCase();
  if (normalized.includes('trivia')) {
    return 'Trivia';
  }
  if (normalized.includes('treasure')) {
    return 'Búsqueda del tesoro';
  }
  return nodeType;
}

export function MissionNodesSummary({
  sessionId,
  teamId,
}: MissionNodesSummaryProps) {
  const [progress, setProgress] = useState<TeamMissionProgress | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    void gameplayService
      .getTeamMissionProgress(sessionId, teamId)
      .then((next) => {
        if (!cancelled) {
          setProgress(next);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(
            err instanceof Error ? err.message : 'No se pudo cargar el resumen de nodos.',
          );
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [sessionId, teamId]);

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>Resumen de nodos</Text>
      {progress ? (
        <Text style={styles.sectionMeta}>
          {progress.completedNodes}/{progress.totalNodes} completados
        </Text>
      ) : null}

      {loading ? <ActivityIndicator color={colors.primary} /> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}

      {!loading && !error && progress ? (
        <View style={styles.list}>
          {progress.nodes.map((node) => (
            <NodeRow key={node.nodeId} node={node} />
          ))}
        </View>
      ) : null}
    </View>
  );
}

function NodeRow({ node }: { node: MissionProgressNode }) {
  return (
    <View
      style={[
        styles.row,
        node.isCompleted ? styles.rowCompleted : styles.rowPending,
      ]}
    >
      <View style={styles.orderBadge}>
        <Text style={styles.orderText}>#{node.executionOrder}</Text>
      </View>
      <View style={styles.nodeInfo}>
        <Text style={styles.nodeTitle}>{node.title}</Text>
        <Text style={styles.nodeMeta}>
          {nodeTypeLabel(node.nodeType)} · {node.baseScore} pts
        </Text>
      </View>
      <Text
        style={[
          styles.status,
          node.isCompleted ? styles.statusDone : styles.statusPending,
        ]}
      >
        {node.isCompleted ? 'Hecho' : 'Pendiente'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: 10,
    marginTop: 8,
  },
  sectionTitle: {
    color: colors.text,
    fontSize: typography.body,
    fontWeight: '700',
  },
  sectionMeta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginTop: -6,
  },
  list: {
    gap: 8,
  },
  row: {
    alignItems: 'center',
    borderRadius: 10,
    borderWidth: 1,
    flexDirection: 'row',
    gap: 10,
    padding: 12,
  },
  rowCompleted: {
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.accent,
  },
  rowPending: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
  },
  orderBadge: {
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderRadius: 8,
    justifyContent: 'center',
    minWidth: 40,
    paddingHorizontal: 6,
    paddingVertical: 6,
  },
  orderText: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '800',
  },
  nodeInfo: {
    flex: 1,
  },
  nodeTitle: {
    color: colors.text,
    fontSize: typography.body,
    fontWeight: '600',
  },
  nodeMeta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginTop: 2,
  },
  status: {
    fontSize: typography.caption,
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  statusDone: {
    color: colors.success,
  },
  statusPending: {
    color: colors.warning,
  },
  error: {
    color: colors.danger,
    fontSize: typography.body,
  },
});
