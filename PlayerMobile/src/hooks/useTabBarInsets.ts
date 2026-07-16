import { useSafeAreaInsets } from 'react-native-safe-area-context';

export const TAB_BAR_CONTENT_HEIGHT = 56;

export function useTabBarInsets() {
  const insets = useSafeAreaInsets();
  const bottomInset = Math.max(insets.bottom, 12);

  return {
    tabBarHeight: TAB_BAR_CONTENT_HEIGHT + bottomInset,
    tabBarPaddingBottom: bottomInset,
    scrollBottomPadding: TAB_BAR_CONTENT_HEIGHT + bottomInset + 16,
  };
}
