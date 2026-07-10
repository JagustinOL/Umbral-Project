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
import { useTeamWorkspace } from '../../../src/hooks/useTeamWorkspace';
import * as liveSessionService from '../../../src/services/liveSessionService';
import type { LiveSessionSummary } from '../../../src/types/liveSession';
import { isSessionTerminal } from '../../../src/types/gameplay';
import { showUserAlert } from '../../../src/utils/confirm';
import { normalizeGuid } from '../../../src/utils/uuid';

export default function SessionsTabScreen() {
  const { session } = useAuth();
  const { team, loadTeam, isLoading: teamLoading } = useTeamWorkspace();
  const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [joiningSessionId, setJoiningSessionId] = useState<string | null>(null);

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
      router.replace('/(main)/no-team');
      return;
    }
    void loadSessions();
  }, [teamId, loadSessions]);

  const handleRefresh = async () => {
    await Promise.all([loadSessions(), loadTeam()]);
  };

  const handleRequestJoin = async (target: LiveSessionSummary) => {
    if (!teamId) {
      return;
    }

    setJoiningSessionId(target.sessionId);
    try {
      await liveSessionService.requestSessionJoin({
        joinCode: target.joinCode,
        teamId,
      });
      await loadTeam();
      showUserAlert(
        'Join requested',
        'Your team leader can confirm once the operator accepts the team.',
      );
    } catch (error) {
      showUserAlert(
        'Join failed',
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
    return null;
  }

  return (
    <InvestigationBackground>
      <ScrollView
        contentContainerStyle={styles.scroll}
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
              onRequestJoin={() => handleRequestJoin(entry)}
            />
          ))
        )}
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: { paddingBottom: 48 },
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
