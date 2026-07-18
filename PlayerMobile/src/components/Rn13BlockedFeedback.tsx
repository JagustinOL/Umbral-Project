import { Alert } from 'react-native';
import { DOMAIN_ERRORS } from '../constants/api';

export function showRn13BlockedAlert(): void {
  Alert.alert(
    'Equipo bloqueado',
    DOMAIN_ERRORS.teamLocked,
  );
}

export function guardLockedAction(
  isLocked: boolean,
  action: () => void | Promise<void>,
): void {
  if (isLocked) {
    showRn13BlockedAlert();
    return;
  }

  void action();
}
