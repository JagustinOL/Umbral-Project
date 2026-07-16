import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';

type ConnectionIndicatorProps = {
  state: 'connected' | 'reconnecting' | 'disconnected';
};

export function ConnectionIndicator({ state }: ConnectionIndicatorProps) {
  if (state === 'connected') {
    return null;
  }

  return (
    <View style={styles.row}>
      <ActivityIndicator color={colors.warning} size="small" />
      <Text style={styles.text}>
        {state === 'reconnecting'
          ? 'Buscando señal… reconectando al motor en vivo'
          : 'Sin conexión en tiempo real — usando respaldo periódico'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    alignItems: 'center',
    flexDirection: 'row',
    gap: 8,
    marginBottom: 12,
  },
  text: {
    color: colors.warning,
    flex: 1,
    fontSize: typography.caption,
    lineHeight: 18,
  },
});
