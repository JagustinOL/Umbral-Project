import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  type PressableProps,
} from 'react-native';
import { colors, typography } from '../constants/theme';
import { showRn13BlockedAlert } from './Rn13BlockedFeedback';

type PrimaryButtonProps = PressableProps & {
  label: string;
  loading?: boolean;
  variant?: 'primary' | 'danger' | 'ghost';
  locked?: boolean;
};

export function PrimaryButton({
  label,
  loading,
  variant = 'primary',
  disabled,
  locked,
  onPress,
  ...pressableProps
}: PrimaryButtonProps) {
  const isDisabled = disabled || loading;

  const handlePress: PressableProps['onPress'] = (event) => {
    if (locked) {
      showRn13BlockedAlert();
      return;
    }
    onPress?.(event);
  };

  return (
    <Pressable
      style={({ pressed }) => [
        styles.base,
        variant === 'primary' && styles.primary,
        variant === 'danger' && styles.danger,
        variant === 'ghost' && styles.ghost,
        pressed && !isDisabled ? styles.pressed : undefined,
        isDisabled || locked ? styles.disabled : undefined,
      ]}
      disabled={isDisabled}
      onPress={handlePress}
      {...pressableProps}
    >
      {loading ? (
        <ActivityIndicator color={colors.text} />
      ) : (
        <Text
          style={[
            styles.label,
            variant === 'ghost' ? styles.ghostLabel : undefined,
          ]}
        >
          {label}
        </Text>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    alignItems: 'center',
    borderRadius: 10,
    justifyContent: 'center',
    minHeight: 48,
    paddingHorizontal: 16,
  },
  primary: {
    backgroundColor: colors.primary,
  },
  danger: {
    backgroundColor: colors.danger,
  },
  ghost: {
    backgroundColor: 'transparent',
    borderColor: colors.border,
    borderWidth: 1,
  },
  pressed: {
    opacity: 0.85,
  },
  disabled: {
    opacity: 0.45,
  },
  label: {
    color: colors.background,
    fontSize: typography.body,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  ghostLabel: {
    color: colors.text,
  },
});
