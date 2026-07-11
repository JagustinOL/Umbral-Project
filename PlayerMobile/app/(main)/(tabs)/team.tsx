import { useFocusEffect } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import {
  Alert,
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
import { useTabBarInsets } from '../../../src/hooks/useTabBarInsets';
import { useTeamWorkspace } from '../../../src/hooks/useTeamWorkspace';
import { confirmDestructive, showUserAlert } from '../../../src/utils/confirm';
import { isTeamLeaderRole, samePlayerRef } from '../../../src/utils/uuid';
import {
  isNonEmpty,
  isValidTeamCode,
  normalizeTeamCode,
} from '../../../src/utils/validation';

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
    createTeam,
    submitJoin,
  } = useTeamWorkspace();
  const { scrollBottomPadding } = useTabBarInsets();

  const [renameValue, setRenameValue] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [teamName, setTeamName] = useState('');
  const [joinCode, setJoinCode] = useState('');
  const [loadingAction, setLoadingAction] = useState<'create' | 'join' | null>(
    null,
  );

  const hasTeam = Boolean(session?.teamId);
  const hasPendingJoin = Boolean(session?.pendingTeamId);

  useFocusEffect(
    useCallback(() => {
      void refreshSession();
    }, [refreshSession]),
  );

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

  const handleCreateTeam = async () => {
    if (!isNonEmpty(teamName)) {
      Alert.alert('Validación', 'El nombre del equipo no puede estar vacío.');
      return;
    }

    setLoadingAction('create');
    try {
      await createTeam(teamName);
      setTeamName('');
    } catch (error) {
      Alert.alert(
        'Error al crear equipo',
        error instanceof Error ? error.message : 'No se pudo crear el equipo.',
      );
    } finally {
      setLoadingAction(null);
    }
  };

  const handleJoinTeam = async () => {
    const code = normalizeTeamCode(joinCode);
    if (!isValidTeamCode(code)) {
      Alert.alert(
        'Validación',
        'El código debe tener exactamente 6 caracteres alfanuméricos.',
      );
      return;
    }

    setLoadingAction('join');
    try {
      await submitJoin(code);
      Alert.alert(
        'Solicitud enviada',
        'Tu solicitud fue enviada. Espera a que el líder del equipo la apruebe.',
      );
      setJoinCode('');
    } catch (error) {
      Alert.alert(
        'Error al unirse',
        error instanceof Error
          ? error.message
          : 'No se pudo enviar la solicitud.',
      );
    } finally {
      setLoadingAction(null);
    }
  };

  if (!hasTeam) {
    return (
      <InvestigationBackground>
        <ScrollView
          contentContainerStyle={[
            styles.scroll,
            { paddingBottom: scrollBottomPadding },
          ]}
        >
          <Text style={styles.screenTitle}>Equipo</Text>
          <Text style={styles.greeting}>
            Bienvenido, {session?.player.firstName}
          </Text>
          <Text style={styles.noTeamTitle}>Sin equipo activo</Text>
          <Text style={styles.subtitle}>
            Crea una nueva unidad de investigación o únete a un equipo existente
            con un código de acceso de 6 caracteres.
          </Text>
          {session?.pendingTeamId ? (
            <Text style={styles.pendingNotice}>
              Ya tienes una solicitud de unión pendiente. Espera a que el líder
              del equipo la apruebe antes de enviar otra.
            </Text>
          ) : null}

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Crear equipo</Text>
            <FormTextField
              label="Nombre del equipo"
              value={teamName}
              onChangeText={setTeamName}
              autoCapitalize="words"
            />
            <PrimaryButton
              label="Crear equipo"
              loading={loadingAction === 'create'}
              locked={hasPendingJoin}
              onPress={handleCreateTeam}
            />
          </View>

          <View style={styles.divider} />

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Unirse a un equipo</Text>
            <FormTextField
              label="Código de equipo (6 caracteres)"
              value={joinCode}
              onChangeText={(value) => setJoinCode(normalizeTeamCode(value))}
              maxLength={6}
              autoCapitalize="characters"
            />
            <PrimaryButton
              label="Enviar solicitud"
              variant="ghost"
              loading={loadingAction === 'join'}
              locked={hasPendingJoin}
              onPress={handleJoinTeam}
            />
          </View>
        </ScrollView>
      </InvestigationBackground>
    );
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
        contentContainerStyle={[
          styles.scroll,
          { paddingBottom: scrollBottomPadding },
        ]}
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
  scroll: {},
  screenTitle: {
    color: colors.textMuted,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 8,
    textTransform: 'uppercase',
  },
  greeting: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.6,
    marginBottom: 6,
    textTransform: 'uppercase',
  },
  noTeamTitle: {
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
  divider: {
    backgroundColor: colors.border,
    height: 1,
    marginVertical: 20,
  },
  error: {
    color: colors.danger,
    fontSize: typography.body,
    marginBottom: 16,
  },
});
