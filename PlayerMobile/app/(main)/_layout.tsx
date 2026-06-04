import { Redirect, Stack } from 'expo-router';
import { useAuth } from '../../src/hooks/useAuth';

export default function MainLayout() {
  const { session, isLoading } = useAuth();

  if (!isLoading && !session) {
    return <Redirect href="/(auth)/login" />;
  }

  return <Stack screenOptions={{ headerShown: false }} />;
}
