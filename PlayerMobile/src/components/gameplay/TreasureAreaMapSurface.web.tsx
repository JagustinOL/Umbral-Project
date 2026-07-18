import { useEffect, useRef } from 'react';
import { createElement } from 'react';
import { StyleSheet, View } from 'react-native';
import L from 'leaflet';
import {
  TREASURE_AREA_RADIUS_METERS,
  TREASURE_MAP_HEIGHT,
} from './treasureMapHtml';

type TreasureAreaMapSurfaceProps = {
  latitude: number;
  longitude: number;
};

const LEAFLET_CSS_ID = 'umbral-leaflet-css';

function ensureLeafletCss() {
  if (typeof document === 'undefined') {
    return;
  }
  if (document.getElementById(LEAFLET_CSS_ID)) {
    return;
  }
  const link = document.createElement('link');
  link.id = LEAFLET_CSS_ID;
  link.rel = 'stylesheet';
  link.href = 'https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/leaflet.css';
  document.head.appendChild(link);
}

/** Expo web: Leaflet en el DOM (evita iframe/WebView). */
export function TreasureAreaMapSurface({
  latitude,
  longitude,
}: TreasureAreaMapSurfaceProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    ensureLeafletCss();
    const el = containerRef.current;
    if (!el) {
      return;
    }

    const map = L.map(el, {
      zoomControl: true,
      attributionControl: true,
    }).setView([latitude, longitude], 17);

    L.tileLayer(
      'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png',
      {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20,
      },
    ).addTo(map);

    const circle = L.circle([latitude, longitude], {
      radius: TREASURE_AREA_RADIUS_METERS,
      color: '#3d9eff',
      fillColor: '#3d9eff',
      fillOpacity: 0.25,
      weight: 2,
    }).addTo(map);

    const refresh = () => {
      map.invalidateSize(true);
      map.fitBounds(circle.getBounds(), { padding: [28, 28], maxZoom: 18 });
    };

    refresh();
    const t1 = window.setTimeout(refresh, 50);
    const t2 = window.setTimeout(refresh, 250);
    const t3 = window.setTimeout(refresh, 600);

    return () => {
      window.clearTimeout(t1);
      window.clearTimeout(t2);
      window.clearTimeout(t3);
      map.remove();
    };
  }, [latitude, longitude]);

  return (
    <View style={styles.fill}>
      {createElement('div', {
        ref: containerRef,
        style: {
          width: '100%',
          height: TREASURE_MAP_HEIGHT,
        },
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  fill: {
    flex: 1,
    height: TREASURE_MAP_HEIGHT,
  },
});
