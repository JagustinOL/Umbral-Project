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
import { PrimaryButton } from '../../../src/components/PrimaryButton';
import { colors, typography } from '../../../src/constants/theme';
import { useAuth } from '../../../src/hooks/useAuth';
import { useTabBarInsets } from '../../../src/hooks/useTabBarInsets';
import { useTeamWorkspace } from '../../../src/hooks/useTeamWorkspace';
import * as liveSessionService from '../../../src/services/liveSessionService';
import {
  connectLiveSessionHub,
  disconnectLiveSessionHub,
} from '../../../src/services/signalRService';
import type { LiveSessionSummary } from '../../../src/types/liveSession';
import { isSessionTerminal } from '../../../src/types/gameplay';
import { showUserAlert } from '../../../src/utils/confirm';
import { normalizeGuid } from '../../../src/utils/uuid';

export default function SessionsTabScreen() {
  const { session } = useAuth();
  const { team, loadTeam, isLoading: teamLoading } = useTeamWorkspace();
  const { scrollBottomPadding } = useTabBarInsets();
  const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [joiningSessionId, setJoiningSessionId] = useState<string | null>(null);
  const [requestedSessionIds, setRequestedSessionIds] = useState<string[]>([]);
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);

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

  useEffect(() => {
    if (!currentSessionRef) {
      return;
    }
    setRequestedSessionIds((current) =>
      current.some(
        (id) =>
          normalizeGuid(id).toLowerCase() ===
          normalizeGuid(currentSessionRef).toLowerCase(),
      )
        ? current
        : [...current, currentSessionRef],
    );
  }, [currentSessionRef]);

  useEffect(() => {
    const awaitingSessionId = pendingSessionId ?? (currentSessionRef && !isLocked ? currentSessionRef : null);
    if (!awaitingSessionId || !teamId) {
      return;
    }

    void connectLiveSessionHub(awaitingSessionId, {
      onJoinRequestResolved: (payload) => {
        if (normalizeGuid(payload.teamId).toLowerCase() !== normalizeGuid(teamId).toLowerCase()) {
          return;
        }
        setPendingSessionId(null);
        void Promise.all([loadTeam(), loadSessions()]);
        showUserAlert(
          payload.decision.toLowerCase() === 'approve' ? 'Solicitud aprobada' : 'Solicitud rechazada',
          payload.decision.toLowerCase() === 'approve'
            ? 'Tu equipo ya puede entrar a la misión cuando la sesión esté activa.'
            : 'El operador rechazó la solicitud de tu equipo.',
        );
      },
    }, teamId);

    return () => {
      void disconnectLiveSessionHub();
    };
  }, [currentSessionRef, isLocked, loadSessions, loadTeam, pendingSessionId, teamId]);

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

        {!canEnterMission && (pendingSessionId || (currentSessionRef && !isLocked)) ? (
          <View style={styles.liveCard}>
            <Text style={styles.liveEyebrow}>SOLICITUD PENDIENTE</Text>
            <Text style={styles.liveTitle}>Esperando aprobación</Text>
            <Text style={styles.liveMeta}>
              El operador debe aprobar la unión de tu equipo antes de que puedan jugar.
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

        {isLoading ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : sessions.length === 0 ? (
          <Text style={styles.empty}>
            No hay sesiones activas en este momento. El operador debe iniciar
            una misión.
          </Text>
        ) : (
          sessions.map((entry) => (
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
