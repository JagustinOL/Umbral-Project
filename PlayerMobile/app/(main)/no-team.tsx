import { Redirect } from 'expo-router';

/** Ruta legacy: la UI sin equipo vive ahora en la pestaña Equipo. */
export default function NoTeamScreen() {
  return <Redirect href="/(main)/(tabs)/team" />;
}
