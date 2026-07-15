import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../../constants/theme';
import { TreasureAreaMapSurface } from './TreasureAreaMapSurface';
import {
  TREASURE_AREA_RADIUS_METERS,
  TREASURE_MAP_HEIGHT,
} from './treasureMapHtml';

type TreasureAreaMapProps = {
  latitude: number;
  longitude: number;
};

export function TreasureAreaMap({ latitude, longitude }: TreasureAreaMapProps) {
  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  return (
    <View style={styles.wrapper}>
      <Text style={styles.label}>Área de juego ({TREASURE_AREA_RADIUS_METERS} m)</Text>
      <View style={styles.mapFrame}>
        <TreasureAreaMapSurface latitude={latitude} longitude={longitude} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    marginTop: 12,
    marginBottom: 4,
    gap: 8,
  },
  label: {
    color: colors.textMuted,
    fontSize: typography.caption,
    fontWeight: '600',
  },
  mapFrame: {
    height: TREASURE_MAP_HEIGHT,
    borderRadius: 10,
    overflow: 'hidden',
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceElevated,
  },
});
