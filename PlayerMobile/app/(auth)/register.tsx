import { Link, router } from 'expo-router';
import { useState } from 'react';
import {
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
} from 'react-native';
import { FormTextField } from '../../src/components/FormTextField';
import { InvestigationBackground } from '../../src/components/InvestigationBackground';
import { PasswordStrengthIndicator } from '../../src/components/PasswordStrengthIndicator';
import { PrimaryButton } from '../../src/components/PrimaryButton';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import {
  isNonEmpty,
  isPasswordMinLength,
  isValidEmail,
} from '../../src/utils/validation';

export default function RegisterScreen() {
  const { register } = useAuth();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = () => {
    const nextErrors: Record<string, string> = {};
    if (!isNonEmpty(firstName)) {
      nextErrors.firstName = 'First name is required.';
    }
    if (!isNonEmpty(lastName)) {
      nextErrors.lastName = 'Last name is required.';
    }
    if (!isValidEmail(email)) {
      nextErrors.email = 'Enter a valid email address.';
    }
    if (!isPasswordMinLength(password)) {
      nextErrors.password = 'Password must be at least 8 characters.';
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleRegister = async () => {
    if (!validate()) {
      return;
    }

    setLoading(true);
    try {
      await register({ firstName, lastName, email, password });
      router.replace('/');
    } catch (error) {
      Alert.alert(
        'Registration failed',
        error instanceof Error ? error.message : 'Unable to register player.',
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <InvestigationBackground>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.flex}
      >
        <ScrollView contentContainerStyle={styles.scroll}>
          <Text style={styles.title}>Create Player</Text>
          <Text style={styles.subtitle}>HU · Register new investigator</Text>

          <FormTextField
            label="First Name"
            value={firstName}
            onChangeText={setFirstName}
            autoCapitalize="words"
            error={errors.firstName}
          />
          <FormTextField
            label="Last Name"
            value={lastName}
            onChangeText={setLastName}
            autoCapitalize="words"
            error={errors.lastName}
          />
          <FormTextField
            label="Email"
            value={email}
            onChangeText={setEmail}
            keyboardType="email-address"
            error={errors.email}
          />
          <FormTextField
            label="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            error={errors.password}
          />
          <PasswordStrengthIndicator password={password} />

          <PrimaryButton
            label="Register"
            loading={loading}
            onPress={handleRegister}
          />

          <Link href="/(auth)/login" style={styles.link}>
            Back to login
          </Link>
        </ScrollView>
      </KeyboardAvoidingView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  scroll: { flexGrow: 1, paddingBottom: 40 },
  title: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '700',
    marginBottom: 6,
  },
  subtitle: {
    color: colors.textMuted,
    fontSize: typography.body,
    marginBottom: 24,
  },
  link: {
    color: colors.primary,
    fontSize: typography.body,
    marginTop: 18,
    textAlign: 'center',
  },
});
