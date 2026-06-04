import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';

export function TeamLockedBadge({ isLocked }: { isLocked: boolean }) {
  return (
    <View
      style={[
        styles.badge,
        isLocked ? styles.locked : styles.unlocked,
      ]}
    >
      <Text style={styles.label}>
        {isLocked ? 'LOCKED · RN-13 ACTIVE' : 'UNLOCKED · EDITABLE'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    alignSelf: 'flex-start',
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  locked: {
    backgroundColor: '#3f1f24',
    borderColor: colors.danger,
    borderWidth: 1,
  },
  unlocked: {
    backgroundColor: '#143024',
    borderColor: colors.success,
    borderWidth: 1,
  },
  label: {
    color: colors.text,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 0.8,
  },
});
