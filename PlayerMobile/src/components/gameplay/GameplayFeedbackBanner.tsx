import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';

type GameplayFeedbackBannerProps = {
  tone: 'success' | 'error' | 'info';
  title: string;
  message: string;
};

const toneStyles = {
  success: {
    backgroundColor: '#1f3d2a',
    borderColor: colors.accent,
    titleColor: colors.accent,
  },
  error: {
    backgroundColor: '#3d1f1f',
    borderColor: colors.danger,
    titleColor: colors.danger,
  },
  info: {
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.border,
    titleColor: colors.text,
  },
} as const;

export function GameplayFeedbackBanner({
  tone,
  title,
  message,
}: GameplayFeedbackBannerProps) {
  const palette = toneStyles[tone];

  return (
    <View
      style={[
        styles.banner,
        { backgroundColor: palette.backgroundColor, borderColor: palette.borderColor },
      ]}
    >
      <Text style={[styles.title, { color: palette.titleColor }]}>{title}</Text>
      <Text style={styles.message}>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  banner: {
    borderRadius: 10,
    borderWidth: 1,
    marginBottom: 16,
    padding: 12,
  },
  title: {
    fontSize: typography.body,
    fontWeight: '700',
    marginBottom: 4,
  },
  message: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
  },
});
