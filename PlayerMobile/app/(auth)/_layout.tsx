import { Stack, Redirect } from 'expo-router';
import { useAuth } from '../../src/hooks/useAuth';

export default function AuthLayout() {
  const { session, isLoading } = useAuth();

  if (!isLoading && session) {
    if (session.teamId) {
      return <Redirect href="/(main)/(tabs)/team" />;
    }
    return <Redirect href="/(main)/no-team" />;
  }

  return <Stack screenOptions={{ headerShown: false }} />;
}
