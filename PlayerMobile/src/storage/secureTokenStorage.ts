import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

const ACCESS_TOKEN_KEY = 'umbral_access_token';
const SESSION_KEY = 'umbral_player_session';

function isWebStorage(): boolean {
  return Platform.OS === 'web';
}

async function setItem(key: string, value: string): Promise<void> {
  if (isWebStorage()) {
    globalThis.localStorage?.setItem(key, value);
    return;
  }

  await SecureStore.setItemAsync(key, value);
}

async function getItem(key: string): Promise<string | null> {
  if (isWebStorage()) {
    return globalThis.localStorage?.getItem(key) ?? null;
  }

  return SecureStore.getItemAsync(key);
}

async function removeItem(key: string): Promise<void> {
  if (isWebStorage()) {
    globalThis.localStorage?.removeItem(key);
    return;
  }

  await SecureStore.deleteItemAsync(key);
}

export async function saveAccessToken(token: string): Promise<void> {
  await setItem(ACCESS_TOKEN_KEY, token);
}

export async function getAccessToken(): Promise<string | null> {
  return getItem(ACCESS_TOKEN_KEY);
}

export async function clearAccessToken(): Promise<void> {
  await removeItem(ACCESS_TOKEN_KEY);
}

export async function saveSessionJson(payload: string): Promise<void> {
  await setItem(SESSION_KEY, payload);
}

export async function getSessionJson(): Promise<string | null> {
  return getItem(SESSION_KEY);
}

export async function clearSessionJson(): Promise<void> {
  await removeItem(SESSION_KEY);
}
