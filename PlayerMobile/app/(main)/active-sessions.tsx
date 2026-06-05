import { router } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { InvestigationBackground } from '../../src/components/InvestigationBackground';
import { LiveSessionCard } from '../../src/components/LiveSessionCard';
import { PrimaryButton } from '../../src/components/PrimaryButton';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import * as liveSessionService from '../../src/services/liveSessionService';
import * as teamService from '../../src/services/teamService';
import type { TeamDetails } from '../../src/types/team';
import type { LiveSessionSummary } from '../../src/types/liveSession';
import { getSessionJoinBlockReason } from '../../src/types/liveSession';
import { confirmDestructive } from '../../src/utils/confirm';

export default function ActiveSessionsScreen() {
  const { session } = useAuth();
  const teamId = session?.teamId;
  const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
  const [team, setTeam] = useState<TeamDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [joiningSessionId, setJoiningSessionId] = useState<string | null>(null);

  const loadSessions = useCallback(async () => {
    if (!teamId) {
      return;
    }

    setIsLoading(true);
    try {
      const [list, teamDetails] = await Promise.all([
        liveSessionService.getActiveSessions(),
        teamService.getTeamById(teamId),
      ]);
      setSessions(list);
      setTeam(teamDetails);
    } catch (error) {
      Alert.alert(
        'Could not load sessions',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setIsLoading(false);
    }
  }, [teamId]);

  useEffect(() => {
    void loadSessions();
  }, [loadSessions]);

  useEffect(() => {
    if (!teamId) {
      router.replace('/(main)/no-team');
    }
  }, [teamId]);

  const handleRequestJoin = async (entry: LiveSessionSummary) => {
    if (!teamId) {
      return;
    }

    const blockReason = getSessionJoinBlockReason({
      sessionStatus: entry.status,
      teamIsLocked: team?.isLocked ?? false,
      teamCurrentSessionRef: team?.currentSessionRef ?? null,
      targetSessionId: entry.sessionId,
    });

    if (blockReason) {
      Alert.alert('Cannot join session', blockReason);
      return;
    }

    const confirmed = await confirmDestructive(
      'Join live session',
      `Register your team for "${entry.missionTitle ?? 'this mission'}"? The operator must approve participation.`,
      'Submit',
    );
    if (!confirmed) {
      return;
    }

    setJoiningSessionId(entry.sessionId);
    try {
      await liveSessionService.requestSessionJoin({
        joinCode: entry.joinCode,
        teamId,
      });
      Alert.alert(
        'Request sent',
        'Your team was registered for this session. Wait for the operator to start the game.',
      );
    } catch (error) {
      Alert.alert(
        'Join request failed',
        error instanceof Error ? error.message : 'Unable to join session.',
      );
    } finally {
      setJoiningSessionId(null);
    }
  };

  if (!teamId) {
    return null;
  }

  return (
    <InvestigationBackground>
      <ScrollView
        contentContainerStyle={styles.scroll}
        refreshControl={
          <RefreshControl
            refreshing={isLoading}
            onRefresh={loadSessions}
            tintColor={colors.primary}
          />
        }
      >
        <Text style={styles.title}>Missions with live sessions</Text>
        <Text style={styles.subtitle}>
          Browse open sessions and submit a join request for your team. Operator
          approval happens in another app.
        </Text>

        {isLoading && sessions.length === 0 ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : null}

        {!isLoading && sessions.length === 0 ? (
          <Text style={styles.empty}>No live sessions available right now.</Text>
        ) : null}

        {team?.isLocked ? (
          <Text style={styles.teamLocked}>
            Your team is in an active live session (RN-13). Join requests to other
            sessions are disabled until the operator ends the current game.
          </Text>
        ) : null}

        {sessions.map((entry) => (
          <LiveSessionCard
            key={entry.sessionId}
            session={entry}
            teamIsLocked={team?.isLocked ?? false}
            teamCurrentSessionRef={team?.currentSessionRef ?? null}
            loading={joiningSessionId === entry.sessionId}
            onRequestJoin={() => handleRequestJoin(entry)}
          />
        ))}

        <PrimaryButton
          label="Back"
          variant="ghost"
          onPress={() => router.back()}
        />
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: {
    paddingBottom: 48,
  },
  title: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '700',
    marginBottom: 8,
  },
  subtitle: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 20,
  },
  empty: {
    color: colors.textMuted,
    fontSize: typography.body,
    marginBottom: 20,
  },
  teamLocked: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 16,
    padding: 12,
    borderColor: colors.border,
    borderRadius: 8,
    borderWidth: 1,
    backgroundColor: colors.surface,
  },
});
