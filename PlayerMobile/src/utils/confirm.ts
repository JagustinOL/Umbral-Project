import { Alert, Platform } from 'react-native';

/**
 * Alertas de error/éxito compatibles con Expo Web (Alert.alert a menudo no se muestra).
 */
export function showUserAlert(title: string, message: string): void {
  if (Platform.OS === 'web') {
    globalThis.alert(`${title}\n\n${message}`);
    return;
  }

  Alert.alert(title, message);
}

/**
 * Confirmación destructiva compatible con Expo Web (Alert.alert a veces no ejecuta onPress).
 */
export function confirmDestructive(
  title: string,
  message: string,
  confirmLabel = 'Confirm',
): Promise<boolean> {
  if (Platform.OS === 'web') {
    return Promise.resolve(
      globalThis.confirm(`${title}\n\n${message}`),
    );
  }

  return new Promise((resolve) => {
    Alert.alert(title, message, [
      { text: 'Cancel', style: 'cancel', onPress: () => resolve(false) },
      {
        text: confirmLabel,
        style: 'destructive',
        onPress: () => resolve(true),
      },
    ]);
  });
}
