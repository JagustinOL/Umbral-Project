import { router } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { FormTextField } from '../../src/components/FormTextField';
import { InvestigationBackground } from '../../src/components/InvestigationBackground';
import { MemberList } from '../../src/components/MemberList';
import { PendingRequestCard } from '../../src/components/PendingRequestCard';
import { PrimaryButton } from '../../src/components/PrimaryButton';
import {
  guardLockedAction,
  showRn13BlockedAlert,
} from '../../src/components/Rn13BlockedFeedback';
import { TeamCodeDisplay } from '../../src/components/TeamCodeDisplay';
import { TeamIdDisplay } from '../../src/components/TeamIdDisplay';
import { TeamLockedBadge } from '../../src/components/TeamLockedBadge';
import { DOMAIN_ERRORS } from '../../src/constants/api';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import { useTeamWorkspace } from '../../src/hooks/useTeamWorkspace';
import { confirmDestructive } from '../../src/utils/confirm';
import { isTeamLeaderRole, samePlayerRef } from '../../src/utils/uuid';

export default function TeamDashboardScreen() {
  const { session, logout, setTeamId, refreshSession } = useAuth();
  const {
    team,
    pendingRequests,
    currentRole,
    isLoading,
    loadTeam,
    processRequest,
    renameTeam,
    disbandTeam,
    leaveTeam,
    removeMember,
  } = useTeamWorkspace();

  const [renameValue, setRenameValue] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (!session?.teamId) {
      router.replace('/(main)/no-team');
    }
  }, [session?.teamId]);

  const playerId = session?.player.playerId;
  const isLeader = useMemo(() => {
    if (playerId && team) {
      const member = team.members.find((entry) =>
        samePlayerRef(entry.playerRef, playerId),
      );
      if (member?.role) {
        return isTeamLeaderRole(member.role);
      }
    }
    return isTeamLeaderRole(currentRole);
  }, [playerId, team, currentRole]);
  const isLocked = team?.isLocked ?? false;

  const managementLocked = isLocked;

  const handleDomainError = (error: unknown) => {
    Alert.alert(
      'Action blocked',
      error instanceof Error ? error.message : 'Operation failed.',
    );
  };

  const handleApprove = async (requestId: string) => {
    guardLockedAction(isLocked, async () => {
      setActionLoading(true);
      try {
        await processRequest(requestId, true);
      } catch (error) {
        handleDomainError(error);
      } finally {
        setActionLoading(false);
      }
    });
  };

  const handleReject = async (requestId: string) => {
    guardLockedAction(isLocked, async () => {
      setActionLoading(true);
      try {
        await processRequest(requestId, false);
      } catch (error) {
        handleDomainError(error);
      } finally {
        setActionLoading(false);
      }
    });
  };

  const handleRename = () => {
    guardLockedAction(isLocked, async () => {
      if (!renameValue.trim()) {
        Alert.alert('Validation', DOMAIN_ERRORS.emptyTeamName);
        return;
      }

      setActionLoading(true);
      try {
        await renameTeam(renameValue);
        setRenameValue('');
        Alert.alert('Success', 'Team name updated.');
      } catch (error) {
        handleDomainError(error);
      } finally {
        setActionLoading(false);
      }
    });
  };

  const handleDisband = async () => {
    if (managementLocked) {
      showRn13BlockedAlert();
      return;
    }

    const confirmed = await confirmDestructive(
      'Disband team',
      'This action cannot be undone.',
      'Disband',
    );
    if (!confirmed) {
      return;
    }

    setActionLoading(true);
    try {
      await disbandTeam();
      await refreshSession();
      router.replace('/(main)/no-team');
    } catch (error) {
      handleDomainError(error);
    } finally {
      setActionLoading(false);
    }
  };

  const handleLeave = async () => {
    if (managementLocked) {
      showRn13BlockedAlert();
      return;
    }

    const confirmed = await confirmDestructive(
      'Leave team',
      'You will lose access to this team workspace.',
      'Leave',
    );
    if (!confirmed) {
      return;
    }

    setActionLoading(true);
    try {
      await leaveTeam();
      await refreshSession();
      router.replace('/(main)/no-team');
    } catch (error) {
      handleDomainError(error);
    } finally {
      setActionLoading(false);
    }
  };

  const handleRemoveMember = async (memberPlayerRef: string) => {
    if (managementLocked) {
      showRn13BlockedAlert();
      return;
    }

    const member = team?.members.find((entry) =>
      samePlayerRef(entry.playerRef, memberPlayerRef),
    );
    const confirmed = await confirmDestructive(
      'Remove member',
      `Remove ${member?.displayName ?? 'this player'} from the team?`,
      'Remove',
    );
    if (!confirmed) {
      return;
    }

    setActionLoading(true);
    try {
      await removeMember(memberPlayerRef);
      if (
        session?.player.playerId &&
        samePlayerRef(memberPlayerRef, session.player.playerId)
      ) {
        await refreshSession();
        router.replace('/(main)/no-team');
      }
    } catch (error) {
      handleDomainError(error);
    } finally {
      setActionLoading(false);
    }
  };

  const pendingSection = useMemo(() => {
    if (!isLeader) {
      return null;
    }

    return (
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Pending join requests</Text>
        {pendingRequests.length === 0 ? (
          <Text style={styles.empty}>No pending join requests.</Text>
        ) : (
          pendingRequests.map((request) => (
            <PendingRequestCard
              key={request.requestId}
              request={request}
              locked={managementLocked}
              actionLoading={actionLoading}
              onApprove={() => handleApprove(request.requestId)}
              onReject={() => handleReject(request.requestId)}
            />
          ))
        )}
      </View>
    );
  }, [
    isLeader,
    pendingRequests,
    managementLocked,
    actionLoading,
    isLocked,
  ]);

  if (!session?.teamId) {
    return null;
  }

  if (!team && !isLoading) {
    return (
      <InvestigationBackground>
        <Text style={styles.error}>Team data unavailable.</Text>
        <PrimaryButton label="Retry" onPress={loadTeam} />
      </InvestigationBackground>
    );
  }

  return (
    <InvestigationBackground>
      <ScrollView
        contentContainerStyle={styles.scroll}
        refreshControl={
          <RefreshControl
            refreshing={isLoading}
            onRefresh={loadTeam}
            tintColor={colors.primary}
          />
        }
      >
        <Text style={styles.teamName}>{team?.name}</Text>
        <TeamLockedBadge isLocked={isLocked} />
        {isLeader && session?.teamId ? (
          <TeamIdDisplay teamId={team?.teamId ?? session.teamId} />
        ) : null}
        {team ? <TeamCodeDisplay teamCode={team.teamCode} /> : null}

        {team ? (
          <MemberList
            members={team.members}
            currentPlayerId={session?.player.playerId}
            canRemoveMembers={isLeader}
            managementLocked={managementLocked}
            removeLoading={actionLoading}
            onRemoveMember={handleRemoveMember}
          />
        ) : null}

        {pendingSection}

        {isLeader ? (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Team Management</Text>
            <FormTextField
              label="Rename Team"
              value={renameValue}
              onChangeText={setRenameValue}
              editable={!managementLocked}
            />
            <PrimaryButton
              label="Rename Team"
              locked={managementLocked}
              loading={actionLoading}
              onPress={handleRename}
            />
            <View style={styles.spacer} />
            <PrimaryButton
              label="Disband Team"
              variant="danger"
              locked={managementLocked}
              onPress={handleDisband}
            />
          </View>
        ) : null}

        {!isLeader ? (
          <View style={styles.section}>
            <PrimaryButton
              label="Leave Team"
              variant="ghost"
              locked={managementLocked}
              onPress={handleLeave}
            />
          </View>
        ) : null}

        <View style={styles.section}>
          <PrimaryButton
            label="Missions & live sessions"
            variant="ghost"
            onPress={() => router.push('/(main)/active-sessions')}
          />
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
        </View>

      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: {
    paddingBottom: 48,
  },
  teamName: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '800',
    marginBottom: 12,
  },
  section: {
    marginTop: 24,
  },
  sectionTitle: {
    color: colors.accent,
    fontSize: typography.subtitle,
    fontWeight: '600',
    marginBottom: 12,
  },
  empty: {
    color: colors.textMuted,
    fontSize: typography.body,
  },
  spacer: {
    height: 10,
  },
  error: {
    color: colors.danger,
    fontSize: typography.body,
    marginBottom: 16,
  },
});
