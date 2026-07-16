import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';
import type { LiveSessionStatus } from '../types/gameplay';

type SessionStatusBannerProps = {
  status: LiveSessionStatus;
  pauseReason?: string | null;
};

export function SessionStatusBanner({
  status,
  pauseReason,
}: SessionStatusBannerProps) {
  const normalized = status.toLowerCase();

  if (normalized === 'active') {
    return null;
  }

  const config = getBannerConfig(normalized, pauseReason);

  return (
    <View style={[styles.banner, { borderColor: config.color }]}>
      <Text style={[styles.label, { color: config.color }]}>{config.label}</Text>
      {config.message ? (
        <Text style={styles.message}>{config.message}</Text>
      ) : null}
    </View>
  );
}

function getBannerConfig(status: string, pauseReason?: string | null) {
  if (status === 'paused') {
    return {
      color: colors.warning,
      label: 'SESIÓN PAUSADA',
      message:
        pauseReason?.trim() ||
        'El operador pausó la misión. Los envíos están bloqueados.',
    };
  }

  if (status === 'finalized') {
    return {
      color: colors.accent,
      label: 'MISIÓN COMPLETADA',
      message: 'Sesión finalizada. Revisa tu resumen final.',
    };
  }

  if (status === 'cancelled') {
    return {
      color: colors.danger,
      label: 'SESIÓN CANCELADA',
      message: pauseReason?.trim() || 'El operador canceló esta sesión.',
    };
  }

  return {
    color: colors.textMuted,
    label: status.toUpperCase(),
    message: 'Esperando a que el operador inicie la misión en vivo.',
  };
}

const styles = StyleSheet.create({
  banner: {
    backgroundColor: colors.surface,
    borderRadius: 10,
    borderWidth: 1,
    marginBottom: 16,
    padding: 14,
  },
  label: {
    fontSize: typography.caption,
    fontWeight: '800',
    letterSpacing: 1,
    marginBottom: 4,
  },
  message: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
