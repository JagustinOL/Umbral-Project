import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';
import type { LiveSessionSummary } from '../types/liveSession';
import { getSessionJoinBlockReason } from '../types/liveSession';
import { PrimaryButton } from './PrimaryButton';

type LiveSessionCardProps = {
  session: LiveSessionSummary;
  teamIsLocked: boolean;
  teamCurrentSessionRef: string | null;
  loading?: boolean;
  joinRequestSent?: boolean;
  onRequestJoin: () => void;
};

export function LiveSessionCard({
  session,
  teamIsLocked,
  teamCurrentSessionRef,
  loading,
  joinRequestSent,
  onRequestJoin,
}: LiveSessionCardProps) {
  const blockReason = getSessionJoinBlockReason({
    sessionStatus: session.status,
    teamIsLocked,
    teamCurrentSessionRef,
    targetSessionId: session.sessionId,
  });
  const joinable = blockReason === null && !joinRequestSent;
  const buttonLabel = joinRequestSent
    ? 'Solicitud enviada'
    : joinable
      ? 'Solicitar unirse'
      : 'Unión no disponible';

  return (
    <View style={styles.card}>
      <Text style={styles.title}>{session.missionTitle ?? 'Mission'}</Text>
      <Text style={styles.meta}>Session · {session.status}</Text>
      <Text style={styles.meta}>
        Code · {session.joinCode}
      </Text>
      {joinRequestSent ? (
        <Text style={styles.blocked}>
          Espera a que el operador apruebe la unión de tu equipo.
        </Text>
      ) : blockReason ? (
        <Text style={styles.blocked}>{blockReason}</Text>
      ) : null}
      <PrimaryButton
        label={buttonLabel}
        variant={joinable ? 'primary' : 'ghost'}
        locked={!joinable}
        loading={loading}
        onPress={onRequestJoin}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 12,
    borderWidth: 1,
    marginBottom: 12,
    padding: 14,
  },
  title: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
    marginBottom: 6,
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 4,
  },
  blocked: {
    color: colors.textMuted,
    fontSize: typography.caption,
    lineHeight: 18,
    marginBottom: 10,
    marginTop: 4,
  },
});
