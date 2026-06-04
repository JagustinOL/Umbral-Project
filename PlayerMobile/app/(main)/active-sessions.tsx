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
import type { LiveSessionSummary } from '../../src/types/liveSession';
import { canRequestSessionJoin } from '../../src/types/liveSession';
import { confirmDestructive } from '../../src/utils/confirm';

export default function ActiveSessionsScreen() {
  const { session } = useAuth();
  const teamId = session?.teamId;
  const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [joiningSessionId, setJoiningSessionId] = useState<string | null>(null);

  const loadSessions = useCallback(async () => {
    setIsLoading(true);
    try {
      const list = await liveSessionService.getActiveSessions();
      setSessions(list);
    } catch (error) {
      Alert.alert(
        'Could not load sessions',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

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

    if (!canRequestSessionJoin(entry.status)) {
      Alert.alert(
        'Registration closed',
        'This session is no longer accepting new teams.',
      );
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

        {sessions.map((entry) => (
          <LiveSessionCard
            key={entry.sessionId}
            session={entry}
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
});
