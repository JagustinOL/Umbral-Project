import {
  StyleSheet,
  Text,
  TextInput,
  View,
  type TextInputProps,
} from 'react-native';
import { colors, typography } from '../constants/theme';

type FormTextFieldProps = TextInputProps & {
  label: string;
  error?: string;
};

export function FormTextField({
  label,
  error,
  ...inputProps
}: FormTextFieldProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        style={[styles.input, error ? styles.inputError : undefined]}
        placeholderTextColor={colors.textMuted}
        autoCapitalize="none"
        {...inputProps}
      />
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: 14,
  },
  label: {
    color: colors.textMuted,
    fontSize: typography.caption,
    marginBottom: 6,
    letterSpacing: 0.6,
    textTransform: 'uppercase',
  },
  input: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    color: colors.text,
    fontSize: typography.body,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  inputError: {
    borderColor: colors.danger,
  },
  error: {
    color: colors.danger,
    fontSize: typography.caption,
    marginTop: 4,
  },
});
