import { Redirect } from 'expo-router';

/** Legacy route → tab Sesiones */
export default function ActiveSessionsRedirect() {
  return <Redirect href="/(main)/(tabs)/sessions" />;
}
