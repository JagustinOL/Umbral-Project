import { Redirect } from 'expo-router';

/** Punto de entrada: siempre enviar a login; login redirige si ya hay sesión. */
export default function IndexRoute() {
  return <Redirect href="/(auth)/login" />;
}
