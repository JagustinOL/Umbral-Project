import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import type { GameplayTab } from '../../types/gameplay';

const TABS: { id: GameplayTab; label: string }[] = [
  { id: 'play', label: 'Jugar' },
  { id: 'hints', label: 'Pistas' },
  { id: 'ranking', label: 'Ranking' },
  { id: 'penalties', label: 'Sanciones' },
  { id: 'summary', label: 'Resumen' },
];

type GameplayTabBarProps = {
  active: GameplayTab;
  onChange: (tab: GameplayTab) => void;
  showSummary: boolean;
};

export function GameplayTabBar({
  active,
  onChange,
  showSummary,
}: GameplayTabBarProps) {
  const visibleTabs = showSummary
    ? TABS
    : TABS.filter((tab) => tab.id !== 'summary');

  return (
    <View style={styles.row}>
      {visibleTabs.map((tab) => {
        const isActive = tab.id === active;
        return (
          <Text
            key={tab.id}
            onPress={() => onChange(tab.id)}
            style={[styles.tab, isActive ? styles.tabActive : undefined]}
          >
            {tab.label}
          </Text>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    marginBottom: 16,
  },
  tab: {
    borderColor: colors.border,
    borderRadius: 999,
    borderWidth: 1,
    color: colors.textMuted,
    fontSize: typography.caption,
    fontWeight: '600',
    overflow: 'hidden',
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  tabActive: {
    backgroundColor: colors.primaryMuted,
    borderColor: colors.primary,
    color: colors.text,
  },
});
