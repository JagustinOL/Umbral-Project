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
import { PrimaryButton } from '../../src/components/PrimaryButton';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import { isValidEmail } from '../../src/utils/validation';

export default function LoginScreen() {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailError, setEmailError] = useState<string | undefined>();
  const [passwordError, setPasswordError] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    const nextEmailError = isValidEmail(email)
      ? undefined
      : 'Enter a valid email address.';
    const nextPasswordError = password ? undefined : 'Password is required.';

    setEmailError(nextEmailError);
    setPasswordError(nextPasswordError);

    if (nextEmailError || nextPasswordError) {
      return;
    }

    setLoading(true);
    try {
      const session = await login(email, password);
      if (session.teamId) {
        router.replace('/(main)/(tabs)/team');
      } else {
        router.replace('/(main)/no-team');
      }
    } catch (error) {
      Alert.alert(
        'Login failed',
        error instanceof Error ? error.message : 'Unable to sign in.',
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
          <Text style={styles.brand}>UMBRAL</Text>
          <Text style={styles.subtitle}>Player access terminal</Text>

          <FormTextField
            label="Email"
            value={email}
            onChangeText={setEmail}
            keyboardType="email-address"
            autoComplete="email"
            error={emailError}
          />
          <FormTextField
            label="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoComplete="password"
            error={passwordError}
          />

          <PrimaryButton
            label="Sign in"
            loading={loading}
            onPress={handleLogin}
          />

          <Link href="/(auth)/register" style={styles.link}>
            Create player account
          </Link>
        </ScrollView>
      </KeyboardAvoidingView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  flex: {
    flex: 1,
  },
  scroll: {
    flexGrow: 1,
    justifyContent: 'center',
    paddingBottom: 40,
  },
  brand: {
    color: colors.accent,
    fontSize: typography.title + 8,
    fontWeight: '800',
    letterSpacing: 4,
    marginBottom: 8,
  },
  subtitle: {
    color: colors.textMuted,
    fontSize: typography.body,
    marginBottom: 28,
  },
  link: {
    color: colors.primary,
    fontSize: typography.body,
    marginTop: 18,
    textAlign: 'center',
  },
});
