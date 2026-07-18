import { StyleSheet, Text, View } from 'react-native';
import { FormTextField } from '../FormTextField';
import { PrimaryButton } from '../PrimaryButton';
import { QrCodeScannerModal } from './QrCodeScannerModal';
import { TreasureAreaMap } from './TreasureAreaMap';
import { colors, typography } from '../../constants/theme';
import type { GpsCoordinate } from '../../types/gameplay';

type TreasureHuntPanelProps = {
  instructions: string | null;
  contentLoading: boolean;
  code: string;
  onChangeCode: (value: string) => void;
  canSubmit: boolean;
  loading: boolean;
  scannerOpen: boolean;
  onOpenScanner: () => void;
  onCloseScanner: () => void;
  onScanned: (value: string) => void;
  onSubmit: () => void;
  destination: GpsCoordinate | null;
};

export function TreasureHuntPanel({
  instructions,
  contentLoading,
  code,
  onChangeCode,
  canSubmit,
  loading,
  scannerOpen,
  onOpenScanner,
  onCloseScanner,
  onScanned,
  onSubmit,
  destination,
}: TreasureHuntPanelProps) {
  const trimmedInstructions = instructions?.trim() ?? '';

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>Búsqueda del tesoro</Text>

      <View style={styles.instructionsCard}>
        <Text style={styles.instructionsLabel}>Instrucciones</Text>
        {contentLoading && !trimmedInstructions ? (
          <Text style={styles.instructionsPlaceholder}>
            Cargando instrucciones…
          </Text>
        ) : trimmedInstructions ? (
          <Text style={styles.instructionsText}>{trimmedInstructions}</Text>
        ) : (
          <Text style={styles.instructionsPlaceholder}>
            No hay instrucciones disponibles para esta búsqueda.
          </Text>
        )}
      </View>

      <PrimaryButton
        label="Escanear código QR"
        locked={!canSubmit}
        disabled={loading}
        onPress={onOpenScanner}
      />
      <FormTextField
        label="O ingresa el código manualmente"
        value={code}
        onChangeText={onChangeCode}
        autoCapitalize="characters"
        editable={canSubmit && !loading}
      />
      {destination ? (
        <TreasureAreaMap
          latitude={destination.latitude}
          longitude={destination.longitude}
        />
      ) : null}
      <PrimaryButton
        label="Enviar código"
        loading={loading}
        locked={!canSubmit}
        onPress={onSubmit}
      />
      <QrCodeScannerModal
        visible={scannerOpen}
        locked={loading}
        onClose={onCloseScanner}
        onScanned={onScanned}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: 12,
    marginTop: 16,
  },
  sectionTitle: {
    color: colors.text,
    fontSize: typography.body,
    fontWeight: '600',
    marginBottom: 4,
  },
  instructionsCard: {
    backgroundColor: colors.surfaceElevated,
    borderColor: colors.accent,
    borderRadius: 10,
    borderWidth: 1,
    gap: 8,
    padding: 14,
  },
  instructionsLabel: {
    color: colors.accent,
    fontSize: typography.caption,
    fontWeight: '700',
    letterSpacing: 0.6,
    textTransform: 'uppercase',
  },
  instructionsText: {
    color: colors.text,
    fontSize: typography.body,
    lineHeight: 22,
  },
  instructionsPlaceholder: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
  },
});
