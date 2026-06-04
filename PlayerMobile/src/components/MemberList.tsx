import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';
import type { TeamMember } from '../types/team';
import { isTeamLeaderRole, samePlayerRef } from '../utils/uuid';

type MemberListProps = {
  members: TeamMember[];
  currentPlayerId?: string;
  canRemoveMembers?: boolean;
  managementLocked?: boolean;
  removeLoading?: boolean;
  onRemoveMember?: (playerRef: string) => void;
};

export function MemberList({
  members,
  currentPlayerId,
  canRemoveMembers = false,
  managementLocked = false,
  removeLoading = false,
  onRemoveMember,
}: MemberListProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Team Members ({members.length}/4)</Text>
      {members.map((item) => {
        const isSelf = currentPlayerId
          ? samePlayerRef(item.playerRef, currentPlayerId)
          : false;
        const canRemove =
          canRemoveMembers &&
          !managementLocked &&
          !removeLoading &&
          !isSelf &&
          !isTeamLeaderRole(item.role) &&
          Boolean(onRemoveMember);

        return (
          <View key={item.playerRef} style={styles.row}>
            <Text style={styles.name}>{item.displayName}</Text>
            <View style={styles.actions}>
              <View
                style={[
                  styles.roleBadge,
                  isTeamLeaderRole(item.role) ? styles.leader : styles.member,
                ]}
              >
                <Text style={styles.roleText}>{item.role}</Text>
              </View>
              {canRemove ? (
                <Pressable
                  onPress={() => onRemoveMember?.(item.playerRef)}
                  style={({ pressed }) => [
                    styles.removeButton,
                    pressed ? styles.removePressed : undefined,
                  ]}
                >
                  <Text style={styles.removeLabel}>Remove</Text>
                </Pressable>
              ) : null}
            </View>
          </View>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginTop: 8,
  },
  title: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.8,
    marginBottom: 10,
    textTransform: 'uppercase',
  },
  row: {
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  name: {
    color: colors.text,
    flex: 1,
    fontSize: typography.body,
  },
  actions: {
    alignItems: 'center',
    flexDirection: 'row',
  },
  roleBadge: {
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 4,
  },
  leader: {
    backgroundColor: '#2a3f5f',
  },
  member: {
    backgroundColor: '#1f2f28',
  },
  roleText: {
    color: colors.text,
    fontSize: typography.caption,
    fontWeight: '700',
  },
  removeButton: {
    borderColor: colors.danger,
    borderRadius: 8,
    borderWidth: 1,
    marginLeft: 8,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  removePressed: {
    opacity: 0.8,
  },
  removeLabel: {
    color: colors.danger,
    fontSize: typography.caption,
    fontWeight: '700',
  },
});
