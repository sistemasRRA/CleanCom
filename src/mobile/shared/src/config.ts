import Constants from 'expo-constants';
import { Platform } from 'react-native';

/**
 * API address.
 * - EXPO_PUBLIC_API_URL wins when set (builds for testing or production).
 * - In development the API runs on the same PC as Expo, so the app uses the address Expo is served
 *   from: it works on the Android emulator and on a phone on the same Wi-Fi (Expo Go on iPhone).
 * - Fallback: 10.0.2.2 is the PC as seen from the Android emulator.
 */
export function apiUrl(): string {
  const fromEnv = process.env.EXPO_PUBLIC_API_URL;
  if (fromEnv) return fromEnv.replace(/\/$/, '');

  const host = Constants.expoConfig?.hostUri?.split(':')[0];
  if (host && host !== 'localhost' && host !== '127.0.0.1') return `http://${host}:5111`;
  return Platform.OS === 'android' ? 'http://10.0.2.2:5111' : 'http://localhost:5111';
}
