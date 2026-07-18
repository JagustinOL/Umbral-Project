import { useEffect, useState } from 'react';
import {
  Modal,
  Pressable,
  StyleSheet,
  Text,
  View,
  ActivityIndicator,
} from 'react-native';
import {
  CameraView,
  useCameraPermissions,
  type BarcodeScanningResult,
} from 'expo-camera';
import { colors, typography } from '../../constants/theme';
import { PrimaryButton } from '../PrimaryButton';

type QrCodeScannerModalProps = {
  visible: boolean;
  locked?: boolean;
  onClose: () => void;
  onScanned: (value: string) => void;
};

export function QrCodeScannerModal({
  visible,
  locked = false,
  onClose,
  onScanned,
}: QrCodeScannerModalProps) {
  const [permission, requestPermission] = useCameraPermissions();
  const [scanArmed, setScanArmed] = useState(true);

  useEffect(() => {
    if (visible) {
      setScanArmed(true);
    }
  }, [visible]);

  const handleBarcodeScanned = (result: BarcodeScanningResult) => {
    if (!scanArmed || locked) {
      return;
    }

    const value = result.data?.trim();
    if (!value) {
      return;
    }

    setScanArmed(false);
    onScanned(value);
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="fullScreen"
      onRequestClose={onClose}
    >
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Escanear QR del tesoro</Text>
          <Pressable onPress={onClose} hitSlop={12}>
            <Text style={styles.close}>Cerrar</Text>
          </Pressable>
        </View>

        {!permission ? (
          <View style={styles.centered}>
            <ActivityIndicator color={colors.primary} size="large" />
          </View>
        ) : !permission.granted ? (
          <View style={styles.centered}>
            <Text style={styles.message}>
              Necesitamos acceso a la cámara para leer el código QR del tesoro.
            </Text>
            <PrimaryButton label="Permitir cámara" onPress={() => void requestPermission()} />
            <PrimaryButton label="Cancelar" variant="ghost" onPress={onClose} />
          </View>
        ) : (
          <View style={styles.cameraWrap}>
            <CameraView
              style={StyleSheet.absoluteFillObject}
              facing="back"
              barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
              onBarcodeScanned={
                scanArmed && !locked ? handleBarcodeScanned : undefined
              }
            />
            <View style={styles.overlay} pointerEvents="none">
              <View style={styles.frame} />
              <Text style={styles.hint}>
                {locked
                  ? 'Validando código…'
                  : 'Centra el QR dentro del marco'}
              </Text>
            </View>
          </View>
        )}
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.background,
    flex: 1,
  },
  header: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 52,
    paddingBottom: 12,
  },
  title: {
    color: colors.text,
    fontSize: typography.subtitle,
    fontWeight: '700',
  },
  close: {
    color: colors.primary,
    fontSize: typography.body,
    fontWeight: '600',
  },
  centered: {
    flex: 1,
    gap: 12,
    justifyContent: 'center',
    paddingHorizontal: 24,
  },
  message: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 8,
    textAlign: 'center',
  },
  cameraWrap: {
    flex: 1,
    margin: 16,
    marginBottom: 32,
    overflow: 'hidden',
    borderRadius: 16,
  },
  overlay: {
    ...StyleSheet.absoluteFillObject,
    alignItems: 'center',
    justifyContent: 'center',
  },
  frame: {
    borderColor: colors.accent,
    borderRadius: 16,
    borderWidth: 2,
    height: 220,
    width: 220,
  },
  hint: {
    color: colors.text,
    fontSize: typography.caption,
    fontWeight: '600',
    marginTop: 16,
    textAlign: 'center',
  },
});
