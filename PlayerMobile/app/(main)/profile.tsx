import { Redirect } from 'expo-router';

/** Legacy route → tab Perfil */
export default function ProfileRedirect() {
  return <Redirect href="/(main)/(tabs)/profile" />;
}
