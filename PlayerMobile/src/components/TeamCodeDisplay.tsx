import * as Clipboard from 'expo-clipboard';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';

export function TeamCodeDisplay({ teamCode }: { teamCode: string }) {
  const handleCopy = async () => {
    await Clipboard.setStringAsync(teamCode);
    Alert.alert('Copied', 'Team access code copied to clipboard.');
  };

  return (
    <Pressable onPress={handleCopy} style={styles.container}>
      <Text style={styles.caption}>ACCESS CODE · TAP TO COPY</Text>
      <Text style={styles.code}>{teamCode}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.primaryMuted,
    borderRadius: 12,
    borderWidth: 1,
    marginVertical: 16,
    padding: 16,
    width: '100%',
  },
  caption: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 1,
    marginBottom: 8,
  },
  code: {
    color: colors.accent,
    fontSize: 32,
    fontWeight: '800',
    letterSpacing: 6,
  },
});
