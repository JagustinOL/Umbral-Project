import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';
import type { LiveSessionSummary } from '../types/liveSession';
import { canRequestSessionJoin } from '../types/liveSession';
import { PrimaryButton } from './PrimaryButton';

type LiveSessionCardProps = {
  session: LiveSessionSummary;
  loading?: boolean;
  onRequestJoin: () => void;
};

export function LiveSessionCard({
  session,
  loading,
  onRequestJoin,
}: LiveSessionCardProps) {
  const joinable = canRequestSessionJoin(session.status);

  return (
    <View style={styles.card}>
      <Text style={styles.title}>{session.missionTitle ?? 'Mission'}</Text>
      <Text style={styles.meta}>Session · {session.status}</Text>
      <Text style={styles.meta}>
        Code · {session.joinCode}
      </Text>
      <PrimaryButton
        label={joinable ? 'Request session join' : 'Registration closed'}
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
});
