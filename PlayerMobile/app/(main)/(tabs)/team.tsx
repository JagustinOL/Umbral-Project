import { router } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import {
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { FormTextField } from '../../../src/components/FormTextField';
import { InvestigationBackground } from '../../../src/components/InvestigationBackground';
import { MemberList } from '../../../src/components/MemberList';
import { PendingRequestCard } from '../../../src/components/PendingRequestCard';
import { PrimaryButton } from '../../../src/components/PrimaryButton';
import {
  guardLockedAction,
  showRn13BlockedAlert,
} from '../../../src/components/Rn13BlockedFeedback';
import { TeamCodeDisplay } from '../../../src/components/TeamCodeDisplay';
import { TeamIdDisplay } from '../../../src/components/TeamIdDisplay';
import { TeamLockedBadge } from '../../../src/components/TeamLockedBadge';
import { DOMAIN_ERRORS } from '../../../src/constants/api';
import { colors, typography } from '../../../src/constants/theme';
import { useAuth } from '../../../src/hooks/useAuth';
import { useTeamWorkspace } from '../../../src/hooks/useTeamWorkspace';
import { confirmDestructive, showUserAlert } from '../../../src/utils/confirm';
import { isTeamLeaderRole, samePlayerRef } from '../../../src/utils/uuid';

export default function TeamTabScreen() {
  const { session, refreshSession } = useAuth();
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
    showUserAlert(
      'Action blocked',
      error instanceof Error ? error.message : 'Operation failed.',
    );
  };

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
        <Text style={styles.screenTitle}>Equipo</Text>
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
            onRemoveMember={async (memberPlayerRef) => {
              if (managementLocked) {
                showRn13BlockedAlert();
                return;
              }
              setActionLoading(true);
              try {
                await removeMember(memberPlayerRef);
              } catch (error) {
                handleDomainError(error);
              } finally {
                setActionLoading(false);
              }
            }}
          />
        ) : null}

        {isLeader ? (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Solicitudes pendientes</Text>
            {pendingRequests.length === 0 ? (
              <Text style={styles.empty}>Sin solicitudes.</Text>
            ) : (
              pendingRequests.map((request) => (
                <PendingRequestCard
                  key={request.requestId}
                  request={request}
                  locked={managementLocked}
                  actionLoading={actionLoading}
                  onApprove={() =>
                    guardLockedAction(isLocked, async () => {
                      setActionLoading(true);
                      try {
                        await processRequest(request.requestId, true);
                      } catch (error) {
                        handleDomainError(error);
                      } finally {
                        setActionLoading(false);
                      }
                    })
                  }
                  onReject={() =>
                    guardLockedAction(isLocked, async () => {
                      setActionLoading(true);
                      try {
                        await processRequest(request.requestId, false);
                      } catch (error) {
                        handleDomainError(error);
                      } finally {
                        setActionLoading(false);
                      }
                    })
                  }
                />
              ))
            )}
          </View>
        ) : null}

        {isLeader ? (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Gestión</Text>
            <FormTextField
              label="Renombrar equipo"
              value={renameValue}
              onChangeText={setRenameValue}
              editable={!managementLocked}
            />
            <PrimaryButton
              label="Guardar nombre"
              locked={managementLocked}
              loading={actionLoading}
              onPress={() =>
                guardLockedAction(isLocked, async () => {
                  if (!renameValue.trim()) {
                    showUserAlert('Validation', DOMAIN_ERRORS.emptyTeamName);
                    return;
                  }
                  setActionLoading(true);
                  try {
                    await renameTeam(renameValue);
                    setRenameValue('');
                  } catch (error) {
                    handleDomainError(error);
                  } finally {
                    setActionLoading(false);
                  }
                })
              }
            />
            <View style={styles.spacer} />
            <PrimaryButton
              label="Disolver equipo"
              variant="danger"
              locked={managementLocked}
              onPress={async () => {
                if (managementLocked) {
                  showRn13BlockedAlert();
                  return;
                }
                const confirmed = await confirmDestructive(
                  'Disband team',
                  'This action cannot be undone.',
                  'Disband',
                );
                if (!confirmed) return;
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
              }}
            />
          </View>
        ) : (
          <View style={styles.section}>
            <PrimaryButton
              label="Abandonar equipo"
              variant="ghost"
              locked={managementLocked}
              onPress={async () => {
                if (managementLocked) {
                  showRn13BlockedAlert();
                  return;
                }
                const confirmed = await confirmDestructive(
                  'Leave team',
                  'You will lose access to this team.',
                  'Leave',
                );
                if (!confirmed) return;
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
              }}
            />
          </View>
        )}
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: { paddingBottom: 48 },
  screenTitle: {
    color: colors.textMuted,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 8,
    textTransform: 'uppercase',
  },
  teamName: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '800',
    marginBottom: 12,
  },
  section: { marginTop: 24 },
  sectionTitle: {
    color: colors.accent,
    fontSize: typography.subtitle,
    fontWeight: '600',
    marginBottom: 12,
  },
  empty: { color: colors.textMuted, fontSize: typography.body },
  spacer: { height: 10 },
  error: {
    color: colors.danger,
    fontSize: typography.body,
    marginBottom: 16,
  },
});
