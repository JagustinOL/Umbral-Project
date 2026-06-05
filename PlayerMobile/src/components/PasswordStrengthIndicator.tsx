import { StyleSheet, Text, View, type DimensionValue } from 'react-native';
import { colors, typography } from '../constants/theme';
import { isPasswordMinLength } from '../utils/validation';

export function PasswordStrengthIndicator({ password }: { password: string }) {
  const isValid = isPasswordMinLength(password);
  const progressWidth = `${Math.min((password.length / 8) * 100, 100)}%`;

  return (
    <View style={styles.container}>
      <View
        style={[
          styles.bar,
          isValid ? styles.barValid : styles.barInvalid,
          !isValid ? { width: progressWidth as DimensionValue } : undefined,
        ]}
      />
      <Text style={[styles.text, isValid ? styles.valid : styles.invalid]}>
        {isValid
          ? 'Password meets minimum length (8+ characters).'
          : 'Minimum 8 characters required.'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: 12,
  },
  bar: {
    borderRadius: 4,
    height: 4,
    marginBottom: 6,
  },
  barValid: {
    backgroundColor: colors.success,
    width: '100%',
  },
  barInvalid: {
    backgroundColor: colors.border,
  },
  text: {
    fontSize: typography.caption,
  },
  valid: {
    color: colors.success,
  },
  invalid: {
    color: colors.textMuted,
  },
});
