import { Redirect } from 'expo-router';

/** Legacy route → tab Equipo */
export default function TeamDashboardRedirect() {
  return <Redirect href="/(main)/(tabs)/team" />;
}
