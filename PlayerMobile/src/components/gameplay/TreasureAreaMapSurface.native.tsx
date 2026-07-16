import { StyleSheet } from 'react-native';
import { WebView } from 'react-native-webview';
import { buildTreasureMapHtml, TREASURE_MAP_HEIGHT } from './treasureMapHtml';

type TreasureAreaMapSurfaceProps = {
  latitude: number;
  longitude: number;
};

/** iOS / Android (Expo Go / nativo). */
export function TreasureAreaMapSurface({
  latitude,
  longitude,
}: TreasureAreaMapSurfaceProps) {
  const html = buildTreasureMapHtml(latitude, longitude);

  return (
    <WebView
      originWhitelist={['*']}
      source={{
        html,
        // Origen HTTP real: OSM/CDN no bloquean tiles desde about:blank.
        baseUrl: 'https://local.umbral.app/',
      }}
      style={styles.webview}
      scrollEnabled={false}
      setSupportMultipleWindows={false}
      javaScriptEnabled
      domStorageEnabled
      allowsInlineMediaPlayback
      mixedContentMode="always"
      androidLayerType="hardware"
      onLoadEnd={() => undefined}
      injectedJavaScript={`
        (function () {
          try {
            if (window.mapRefresh) { window.mapRefresh(); }
          } catch (e) {}
          true;
        })();
      `}
    />
  );
}

const styles = StyleSheet.create({
  webview: {
    width: '100%',
    height: TREASURE_MAP_HEIGHT,
    backgroundColor: '#e8edf4',
  },
});
