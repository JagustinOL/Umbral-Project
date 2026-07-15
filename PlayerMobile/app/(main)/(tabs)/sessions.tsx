import { router } from 'expo-router';
import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { InvestigationBackground } from '../../../src/components/InvestigationBackground';
import { LiveSessionCard } from '../../../src/components/LiveSessionCard';
import { FormTextField } from '../../../src/components/FormTextField';
import { PrimaryButton } from '../../../src/components/PrimaryButton';
import { colors, typography } from '../../../src/constants/theme';
import { useAuth } from '../../../src/hooks/useAuth';
import { useTabBarInsets } from '../../../src/hooks/useTabBarInsets';
import { useTeamWorkspace } from '../../../src/hooks/useTeamWorkspace';
import * as liveSessionService from '../../../src/services/liveSessionService';
import {
  connectLiveSessionHub,
  disconnectLiveSessionHub,
  isJoinDecisionApproved,
  isJoinDecisionRejected,
} from '../../../src/services/signalRService';
import type { LiveSessionSummary } from '../../../src/types/liveSession';
import { isSessionTerminal } from '../../../src/types/gameplay';
import { showUserAlert } from '../../../src/utils/confirm';
import { normalizeGuid } from '../../../src/utils/uuid';
import { normalizeTeamCode } from '../../../src/utils/validation';

type JoinStage = 'idle' | 'pending' | 'approved' | 'rejected';

function sameSessionId(a: string | null | undefined, b: string | null | undefined): boolean {
  if (!a || !b) return false;
  return normalizeGuid(a).toLowerCase() === normalizeGuid(b).toLowerCase();
}

export default function SessionsTabScreen() {
  const { session } = useAuth();
  const { team, loadTeam, isLoading: teamLoading } = useTeamWorkspace();
  const { scrollBottomPadding } = useTabBarInsets();
  const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [joiningSessionId, setJoiningSessionId] = useState<string | null>(null);
  const [requestedSessionIds, setRequestedSessionIds] = useState<string[]>([]);
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);
  const [joinStage, setJoinStage] = useState<JoinStage>('idle');
  const [joinCodeQuery, setJoinCodeQuery] = useState('');

  const teamId = session?.teamId;
  const currentSessionRef = team?.currentSessionRef ?? null;
  const isLocked = team?.isLocked ?? false;

  const liveSession = useMemo(() => {
    if (!currentSessionRef) {
      return null;
    }
    const normalized = normalizeGuid(currentSessionRef).toLowerCase();
    return (
      sessions.find(
        (entry) => normalizeGuid(entry.sessionId).toLowerCase() === normalized,
      ) ?? {
        sessionId: currentSessionRef,
        missionRef: '',
        joinCode: '—',
        status: isLocked ? 'Active' : 'Pending',
        createdAtUtc: '',
        missionTitle: 'Misión en curso',
      }
    );
  }, [currentSessionRef, sessions, isLocked]);

  const canEnterMission =
    Boolean(currentSessionRef) &&
    isLocked &&
    liveSession &&
    !isSessionTerminal(liveSession.status);

  // Aprobado y aún no inicia: el equipo ya tiene sessionRef pero no está bloqueado.
  const isApprovedWaitingStart =
    !canEnterMission &&
    (joinStage === 'approved' || (Boolean(currentSessionRef) && !isLocked));

  const isAwaitingApproval =
    !canEnterMission &&
    !isApprovedWaitingStart &&
    (joinStage === 'pending' || Boolean(pendingSessionId));

  const trackedSessionId = pendingSessionId ?? currentSessionRef;

  const availableSessions = useMemo(() => {
    return sessions.filter((entry) => {
      // Ya unido / con solicitud en curso: no listar en "sesiones disponibles".
      if (sameSessionId(entry.sessionId, trackedSessionId)) {
        return false;
      }
      if (
        requestedSessionIds.some((id) => sameSessionId(id, entry.sessionId)) &&
        (joinStage === 'pending' || joinStage === 'approved' || isApprovedWaitingStart)
      ) {
        return false;
      }
      return true;
    });
  }, [
    sessions,
    trackedSessionId,
    requestedSessionIds,
    joinStage,
    isApprovedWaitingStart,
  ]);

  const filteredAvailableSessions = useMemo(() => {
    const query = normalizeTeamCode(joinCodeQuery);
    if (!query) {
      return availableSessions;
    }
    return availableSessions.filter((entry) =>
      normalizeTeamCode(entry.joinCode).includes(query),
    );
  }, [availableSessions, joinCodeQuery]);

  const loadSessions = useCallback(async () => {
    setIsLoading(true);
    try {
      const next = await liveSessionService.getActiveSessions();
      setSessions(next);
    } catch (error) {
      showUserAlert(
        'Could not load sessions',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!teamId) {
      return;
    }
    void loadSessions();
  }, [teamId, loadSessions]);

  // Recuperar estado tras refresh: si el equipo ya está asignado y la sesión no arrancó.
  useEffect(() => {
    if (canEnterMission) {
      setJoinStage('idle');
      setPendingSessionId(null);
      return;
    }
    if (currentSessionRef && !isLocked) {
      setJoinStage('approved');
      setPendingSessionId(null);
      setRequestedSessionIds((current) =>
        current.some((id) => sameSessionId(id, currentSessionRef))
          ? current
          : [...current, currentSessionRef],
      );
    }
  }, [canEnterMission, currentSessionRef, isLocked]);

  useEffect(() => {
    const awaitingSessionId =
      pendingSessionId ??
      (currentSessionRef && !isLocked ? currentSessionRef : null) ??
      (joinStage === 'approved' || joinStage === 'pending' ? trackedSessionId : null);

    if (!awaitingSessionId || !teamId) {
      return;
    }

    void connectLiveSessionHub(
      awaitingSessionId,
      {
        onJoinRequestResolved: (payload) => {
          if (!sameSessionId(payload.teamId, teamId)) {
            return;
          }

          if (isJoinDecisionApproved(payload.decision)) {
            setJoinStage('approved');
            setPendingSessionId(null);
            void Promise.all([loadTeam(), loadSessions()]);
            showUserAlert(
              'Solicitud aprobada',
              'Tu equipo fue aceptado. Espera a que el operador inicie la sesión.',
            );
            return;
          }

          if (isJoinDecisionRejected(payload.decision)) {
            setJoinStage('rejected');
            setPendingSessionId(null);
            setRequestedSessionIds((current) =>
              current.filter((id) => !sameSessionId(id, payload.sessionId)),
            );
            void Promise.all([loadTeam(), loadSessions()]);
            showUserAlert(
              'Solicitud rechazada',
              'El operador rechazó la solicitud de tu equipo.',
            );
          }
        },
        onSessionStateChanged: (payload) => {
          const status = payload.newStatus.trim().toLowerCase();
          if (status === 'cancelled' || status === 'finalized') {
            const closedSessionId = payload.sessionId;
            setJoinStage('idle');
            setPendingSessionId(null);
            setRequestedSessionIds((current) =>
              current.filter((id) => !sameSessionId(id, closedSessionId)),
            );
            void Promise.all([loadTeam(), loadSessions()]);
            showUserAlert(
              status === 'cancelled' ? 'Sesión cancelada' : 'Sesión finalizada',
              payload.reason?.trim() ||
                (status === 'cancelled'
                  ? 'El operador canceló la sesión antes de iniciarla.'
                  : 'El operador finalizó la sesión.'),
            );
            return;
          }

          void Promise.all([loadTeam(), loadSessions()]);
        },
      },
      teamId,
    ).catch((error) => {
      showUserAlert(
        'Conexión en vivo no disponible',
        error instanceof Error
          ? error.message
          : 'No se pudo conectar al hub de la sesión. Recarga o espera la aprobación del operador.',
      );
    });

    return () => {
      void disconnectLiveSessionHub();
    };
  }, [
    currentSessionRef,
    isLocked,
    joinStage,
    loadSessions,
    loadTeam,
    pendingSessionId,
    teamId,
    trackedSessionId,
  ]);

  const handleRefresh = async () => {
    await Promise.all([loadSessions(), loadTeam()]);
  };

  const handleRequestJoin = async (target: LiveSessionSummary) => {
    if (!teamId) {
      return;
    }

    const normalizedTargetId = normalizeGuid(target.sessionId).toLowerCase();
    if (
      joiningSessionId ||
      requestedSessionIds.some(
        (id) => normalizeGuid(id).toLowerCase() === normalizedTargetId,
      )
    ) {
      return;
    }

    setJoiningSessionId(target.sessionId);
    try {
      const result = await liveSessionService.requestSessionJoin({
        joinCode: target.joinCode,
        teamId,
      });
      if (result.status.toLowerCase() === 'pending') {
        setPendingSessionId(result.sessionId);
        setJoinStage('pending');
      }
      setRequestedSessionIds((current) =>
        current.some(
          (id) => normalizeGuid(id).toLowerCase() === normalizedTargetId,
        )
          ? current
          : [...current, target.sessionId],
      );
      await loadTeam();
      showUserAlert(
        'Solicitud enviada',
        'Espera a que el operador apruebe la unión de tu equipo.',
      );
    } catch (error) {
      showUserAlert(
        'No se pudo unir',
        error instanceof Error ? error.message : 'Unable to join session.',
      );
    } finally {
      setJoiningSessionId(null);
    }
  };

  const handleEnterMission = () => {
    if (!currentSessionRef) {
      return;
    }
    router.push(`/(main)/session/${normalizeGuid(currentSessionRef)}`);
  };

  if (!teamId) {
    return (
      <InvestigationBackground>
        <ScrollView
          contentContainerStyle={[
            styles.scroll,
            { paddingBottom: scrollBottomPadding },
          ]}
        >
          <Text style={styles.screenTitle}>Sesiones</Text>
          <Text style={styles.subtitle}>
            Para unirte a misiones activas necesitas formar parte de un equipo.
          </Text>
          <Text style={styles.empty}>
            Ve a la pestaña Equipo para crear uno nuevo o solicitar unirte con
            un código de acceso.
          </Text>
        </ScrollView>
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
            refreshing={isLoading || teamLoading}
            onRefresh={handleRefresh}
            tintColor={colors.primary}
          />
        }
      >
        <Text style={styles.screenTitle}>Sesiones</Text>
        <Text style={styles.subtitle}>
          Únete a una misión activa o entra al tablero en vivo de tu equipo.
        </Text>

        {canEnterMission ? (
          <View style={styles.liveCard}>
            <Text style={styles.liveEyebrow}>MISIÓN EN CURSO</Text>
            <Text style={styles.liveTitle}>
              {liveSession?.missionTitle ?? 'Live session'}
            </Text>
            <Text style={styles.liveMeta}>Estado · {liveSession?.status}</Text>
            <PrimaryButton
              label="Entrar a la misión"
              onPress={handleEnterMission}
            />
          </View>
        ) : null}

        {isAwaitingApproval ? (
          <View style={styles.liveCard}>
            <Text style={styles.liveEyebrow}>SOLICITUD PENDIENTE</Text>
            <Text style={styles.liveTitle}>Esperando aprobación</Text>
            <Text style={styles.liveMeta}>
              El operador debe aprobar la unión de tu equipo antes de que puedan jugar.
            </Text>
          </View>
        ) : null}

        {isApprovedWaitingStart ? (
          <View style={styles.liveCard}>
            <Text style={styles.liveEyebrow}>SOLICITUD APROBADA</Text>
            <Text style={styles.liveTitle}>Listo para jugar</Text>
            <Text style={styles.liveMeta}>
              Tu equipo fue aceptado. Espera a que el operador inicie la sesión.
            </Text>
          </View>
        ) : null}

        {currentSessionRef && isSessionTerminal(liveSession?.status ?? '') ? (
          <View style={styles.liveCard}>
            <Text style={styles.liveEyebrow}>SESIÓN FINALIZADA</Text>
            <Text style={styles.liveTitle}>
              {liveSession?.missionTitle ?? 'Session ended'}
            </Text>
            <PrimaryButton
              label="Ver resumen final"
              variant="ghost"
              onPress={handleEnterMission}
            />
          </View>
        ) : null}

        <Text style={styles.sectionTitle}>Sesiones disponibles</Text>

        <FormTextField
          label="Buscar por código de unión"
          value={joinCodeQuery}
          onChangeText={(value) => setJoinCodeQuery(normalizeTeamCode(value))}
          placeholder="Ej. AB12CD"
          autoCapitalize="characters"
          autoCorrect={false}
          maxLength={6}
        />

        {isLoading ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : availableSessions.length === 0 ? (
          <Text style={styles.empty}>
            {isAwaitingApproval || isApprovedWaitingStart || canEnterMission
              ? 'No hay otras sesiones disponibles. Ya estás vinculado a una misión.'
              : 'No hay sesiones activas en este momento. El operador debe iniciar una misión.'}
          </Text>
        ) : filteredAvailableSessions.length === 0 ? (
          <Text style={styles.empty}>
            No hay sesiones con el código "{joinCodeQuery}". Revisa el código con el operador.
          </Text>
        ) : (
          filteredAvailableSessions.map((entry) => (
            <LiveSessionCard
              key={entry.sessionId}
              session={entry}
              teamIsLocked={isLocked}
              teamCurrentSessionRef={currentSessionRef}
              loading={joiningSessionId === entry.sessionId}
              joinRequestSent={requestedSessionIds.some(
                (id) =>
                  normalizeGuid(id).toLowerCase() ===
                  normalizeGuid(entry.sessionId).toLowerCase(),
              )}
              onRequestJoin={() => handleRequestJoin(entry)}
            />
          ))
        )}
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: {},
  screenTitle: {
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
  liveCard: {
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.accent,
    borderRadius: 12,
    borderWidth: 1,
    marginBottom: 24,
    padding: 16,
  },
  liveEyebrow: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 1,
    marginBottom: 6,
  },
  liveTitle: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
    marginBottom: 6,
  },
  liveMeta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 14,
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
    lineHeight: 22,
  },
});
