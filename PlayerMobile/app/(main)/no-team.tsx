import { router, useFocusEffect } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { FormTextField } from '../../src/components/FormTextField';
import { InvestigationBackground } from '../../src/components/InvestigationBackground';
import { PrimaryButton } from '../../src/components/PrimaryButton';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import { useTeamWorkspace } from '../../src/hooks/useTeamWorkspace';
import {
  isNonEmpty,
  isValidTeamCode,
  normalizeTeamCode,
} from '../../src/utils/validation';

export default function NoTeamScreen() {
  const { session, logout, refreshSession } = useAuth();
  const { createTeam, submitJoin } = useTeamWorkspace();

  useFocusEffect(
    useCallback(() => {
      void refreshSession();
    }, [refreshSession]),
  );

  useEffect(() => {
    if (session?.teamId) {
      router.replace('/(main)/team-dashboard');
    }
  }, [session?.teamId]);
  const [teamName, setTeamName] = useState('');
  const [joinCode, setJoinCode] = useState('');
  const [loadingAction, setLoadingAction] = useState<'create' | 'join' | null>(
    null,
  );
  const hasPendingJoin = Boolean(session?.pendingTeamId);

  const handleCreateTeam = async () => {
    if (!isNonEmpty(teamName)) {
      Alert.alert('Validation', 'Team name cannot be empty.');
      return;
    }

    setLoadingAction('create');
    try {
      await createTeam(teamName);
    } catch (error) {
      Alert.alert(
        'Create team failed',
        error instanceof Error ? error.message : 'Unable to create team.',
      );
    } finally {
      setLoadingAction(null);
    }
  };

  const handleJoinTeam = async () => {
    const code = normalizeTeamCode(joinCode);
    if (!isValidTeamCode(code)) {
      Alert.alert(
        'Validation',
        'Team code must be exactly 6 alphanumeric characters.',
      );
      return;
    }

    setLoadingAction('join');
    try {
      await submitJoin(code);
      Alert.alert(
        'Join request sent',
        'Your request was submitted. Wait for the team leader to approve it.',
      );
      setJoinCode('');
    } catch (error) {
      Alert.alert(
        'Join request failed',
        error instanceof Error ? error.message : 'Unable to submit request.',
      );
    } finally {
      setLoadingAction(null);
    }
  };

  return (
    <InvestigationBackground>
      <ScrollView contentContainerStyle={styles.scroll}>
        <Text style={styles.greeting}>
          Welcome, {session?.player.firstName}
        </Text>
        <Text style={styles.title}>No active team</Text>
        <Text style={styles.subtitle}>
          Create a new investigation unit or join an existing team with a
          6-character access code.
        </Text>
        {session?.pendingTeamId ? (
          <Text style={styles.pendingNotice}>
            You already have a pending join request. Wait for the team leader
            to approve it before sending another.
          </Text>
        ) : null}

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Create Team</Text>
          <FormTextField
            label="Team Name"
            value={teamName}
            onChangeText={setTeamName}
            autoCapitalize="words"
          />
          <PrimaryButton
            label="Create Team"
            loading={loadingAction === 'create'}
            locked={hasPendingJoin}
            onPress={handleCreateTeam}
          />
        </View>

        <View style={styles.divider} />

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Join Team</Text>
          <FormTextField
            label="Team Code (6 chars)"
            value={joinCode}
            onChangeText={(value) => setJoinCode(normalizeTeamCode(value))}
            maxLength={6}
            autoCapitalize="characters"
          />
          <PrimaryButton
            label="Submit Join Request"
            variant="ghost"
            loading={loadingAction === 'join'}
            locked={hasPendingJoin}
            onPress={handleJoinTeam}
          />
        </View>

        <PrimaryButton
          label="My profile"
          variant="ghost"
          onPress={() => router.push('/(main)/profile')}
        />
        <PrimaryButton
          label="Sign out"
          variant="ghost"
          onPress={async () => {
            await logout();
            router.replace('/(auth)/login');
          }}
        />
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: {
    paddingBottom: 48,
  },
  greeting: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.6,
    marginBottom: 6,
    textTransform: 'uppercase',
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
    marginBottom: 24,
  },
  pendingNotice: {
    color: colors.accent,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 20,
  },
  section: {
    marginBottom: 8,
  },
  sectionTitle: {
    color: colors.accent,
    fontSize: typography.subtitle,
    fontWeight: '600',
    marginBottom: 12,
  },
  divider: {
    backgroundColor: colors.border,
    height: 1,
    marginVertical: 20,
  },
});
