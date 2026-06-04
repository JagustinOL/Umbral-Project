import * as Clipboard from 'expo-clipboard';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../constants/theme';

export function TeamIdDisplay({ teamId }: { teamId: string }) {
  const handleCopy = async () => {
    await Clipboard.setStringAsync(teamId);
    Alert.alert('Copied', 'Team ID copied to clipboard (use in Postman as {{teamId}}).');
  };

  return (
    <Pressable onPress={handleCopy} style={styles.container}>
      <Text style={styles.caption}>TEAM ID · TAP TO COPY</Text>
      <Text style={styles.id} selectable>
        {teamId}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderRadius: 10,
    borderWidth: 1,
    marginBottom: 12,
    padding: 12,
    width: '100%',
  },
  caption: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.8,
    marginBottom: 6,
  },
  id: {
    color: colors.text,
    fontFamily: 'monospace',
    fontSize: typography.caption,
  },
});
