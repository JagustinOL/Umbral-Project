import { router } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { FormTextField } from '../../src/components/FormTextField';
import { InvestigationBackground } from '../../src/components/InvestigationBackground';
import { PrimaryButton } from '../../src/components/PrimaryButton';
import { colors, typography } from '../../src/constants/theme';
import { useAuth } from '../../src/hooks/useAuth';
import * as playerApi from '../../src/services/playerApi';
import { confirmDestructive, showUserAlert } from '../../src/utils/confirm';
import { isNonEmpty, isValidEmail } from '../../src/utils/validation';

export default function ProfileScreen() {
  const { session, logout, updatePlayerProfile } = useAuth();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const playerId = session?.player.playerId;

  const loadProfile = useCallback(async () => {
    if (!playerId) {
      return;
    }

    setIsLoading(true);
    try {
      const profile = await playerApi.getPlayerById(playerId);
      setFirstName(profile.firstName);
      setLastName(profile.lastName);
      setEmail(profile.email);
      setIsActive(profile.isActive ?? true);
      await updatePlayerProfile(profile);
    } catch (error) {
      showUserAlert(
        'Could not load profile',
        error instanceof Error ? error.message : 'Request failed.',
      );
    } finally {
      setIsLoading(false);
    }
  }, [playerId, updatePlayerProfile]);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

  const validate = () => {
    const nextErrors: Record<string, string> = {};
    if (!isNonEmpty(firstName)) {
      nextErrors.firstName = 'First name is required.';
    }
    if (!isNonEmpty(lastName)) {
      nextErrors.lastName = 'Last name is required.';
    }
    if (!isValidEmail(email)) {
      nextErrors.email = 'Enter a valid email address.';
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSave = async () => {
    if (!playerId || !validate()) {
      return;
    }

    setIsSaving(true);
    try {
      await playerApi.updatePlayer(playerId, { firstName, lastName, email });
      const refreshed = await playerApi.getPlayerById(playerId);
      await updatePlayerProfile(refreshed);
      showUserAlert('Profile updated', 'Your account details were saved.');
    } catch (error) {
      showUserAlert(
        'Update failed',
        error instanceof Error ? error.message : 'Unable to update profile.',
      );
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeactivate = async () => {
    if (!playerId) {
      return;
    }

    const confirmed = await confirmDestructive(
      'Deactivate account',
      'Your player account will be disabled. You will be signed out and cannot log in again until an operator reactivates you.',
      'Deactivate',
    );
    if (!confirmed) {
      return;
    }

    setIsSaving(true);
    try {
      await playerApi.deactivatePlayer(playerId);
      await logout();
      router.replace('/(auth)/login');
    } catch (error) {
      showUserAlert(
        'Deactivation failed',
        error instanceof Error ? error.message : 'Unable to deactivate account.',
      );
    } finally {
      setIsSaving(false);
    }
  };

  if (!session) {
    return null;
  }

  return (
    <InvestigationBackground>
      <ScrollView contentContainerStyle={styles.scroll}>
        <Text style={styles.title}>My profile</Text>
        <Text style={styles.subtitle}>
          View and update your player account (MissionManagement API).
        </Text>

        {isLoading ? (
          <ActivityIndicator color={colors.primary} size="large" />
        ) : (
          <>
            <View style={styles.statusRow}>
              <Text style={styles.statusLabel}>Account status</Text>
              <Text
                style={[
                  styles.statusBadge,
                  isActive ? styles.statusActive : styles.statusInactive,
                ]}
              >
                {isActive ? 'ACTIVE' : 'INACTIVE'}
              </Text>
            </View>

            <Text style={styles.metaLabel}>PLAYER ID</Text>
            <Text style={styles.metaValue}>{playerId}</Text>

            <FormTextField
              label="First name"
              value={firstName}
              onChangeText={setFirstName}
              autoCapitalize="words"
              error={errors.firstName}
            />
            <FormTextField
              label="Last name"
              value={lastName}
              onChangeText={setLastName}
              autoCapitalize="words"
              error={errors.lastName}
            />
            <FormTextField
              label="Email"
              value={email}
              onChangeText={setEmail}
              keyboardType="email-address"
              autoComplete="email"
              error={errors.email}
            />

            <PrimaryButton
              label="Save changes"
              loading={isSaving}
              onPress={handleSave}
            />

            <View style={styles.section}>
              <Text style={styles.sectionTitle}>Deactivate account</Text>
              <Text style={styles.sectionHint}>
                Disables your player account in Keycloak. This is not the same
                as leaving a team.
              </Text>
              <PrimaryButton
                label="Deactivate account"
                variant="danger"
                locked={!isActive}
                onPress={handleDeactivate}
              />
            </View>
          </>
        )}

        <View style={styles.footer}>
          <PrimaryButton
            label="Back"
            variant="ghost"
            onPress={() => router.back()}
          />
        </View>
      </ScrollView>
    </InvestigationBackground>
  );
}

const styles = StyleSheet.create({
  scroll: {
    paddingBottom: 48,
  },
  title: {
    color: colors.text,
    fontSize: typography.title,
    fontWeight: '700',
    marginBottom: 8,
  },
  subtitle: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 22,
    marginBottom: 24,
  },
  statusRow: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 16,
  },
  statusLabel: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.6,
    textTransform: 'uppercase',
  },
  statusBadge: {
    borderRadius: 999,
    fontSize: typography.caption,
    fontWeight: '700',
    overflow: 'hidden',
    paddingHorizontal: 12,
    paddingVertical: 4,
  },
  statusActive: {
    backgroundColor: '#1f3d2a',
    color: colors.accent,
  },
  statusInactive: {
    backgroundColor: '#3d1f1f',
    color: colors.danger,
  },
  metaLabel: {
    color: colors.textMuted,
    fontSize: typography.caption,
    letterSpacing: 0.8,
    marginBottom: 4,
  },
  metaValue: {
    color: colors.text,
    fontSize: typography.caption,
    marginBottom: 20,
  },
  section: {
    marginTop: 28,
  },
  sectionTitle: {
    color: colors.accent,
    fontSize: typography.subtitle,
    fontWeight: '600',
    marginBottom: 8,
  },
  sectionHint: {
    color: colors.textMuted,
    fontSize: typography.body,
    lineHeight: 20,
    marginBottom: 12,
  },
  footer: {
    marginTop: 24,
  },
});
