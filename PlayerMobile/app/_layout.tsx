import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthNavigationGuard } from '../src/components/AuthNavigationGuard';
import { AuthProvider } from '../src/context/AuthProvider';
import { colors } from '../src/constants/theme';

export const unstable_settings = {
  initialRouteName: '(auth)/login',
};

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <AuthNavigationGuard>
          <StatusBar style="light" />
          <Stack
            screenOptions={{
              headerShown: false,
              contentStyle: { backgroundColor: colors.background },
            }}
          />
        </AuthNavigationGuard>
      </AuthProvider>
    </SafeAreaProvider>
  );
}
