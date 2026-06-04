import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';
import type { JoinRequest } from '../types/team';
import { PrimaryButton } from './PrimaryButton';

type PendingRequestCardProps = {
  request: JoinRequest;
  locked: boolean;
  actionLoading: boolean;
  onApprove: () => void;
  onReject: () => void;
};

export function PendingRequestCard({
  request,
  locked,
  actionLoading,
  onApprove,
  onReject,
}: PendingRequestCardProps) {
  return (
    <View style={styles.card}>
      <Text style={styles.name}>{request.displayName}</Text>
      <Text style={styles.meta}>Join request pending</Text>
      <View style={styles.actions}>
        <View style={styles.actionButton}>
          <PrimaryButton
            label="Approve"
            locked={locked}
            loading={actionLoading}
            onPress={onApprove}
          />
        </View>
        <View style={styles.actionButton}>
          <PrimaryButton
            label="Reject"
            variant="ghost"
            locked={locked}
            loading={actionLoading}
            onPress={onReject}
          />
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    marginBottom: 10,
    padding: 14,
  },
  name: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '600',
  },
  meta: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 12,
    marginTop: 4,
  },
  actions: {
    flexDirection: 'row',
  },
  actionButton: {
    flex: 1,
    marginRight: 8,
  },
});
