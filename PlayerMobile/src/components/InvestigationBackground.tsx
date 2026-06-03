import { StyleSheet, View } from 'react-native';
import { colors } from '../constants/theme';

export function InvestigationBackground({
  children,
}: {
  children: React.ReactNode;
}) {
  return <View style={styles.root}>{children}</View>;
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: colors.background,
    paddingHorizontal: 20,
    paddingTop: 48,
  },
});
