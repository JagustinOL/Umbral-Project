import { getRealmRoles } from './jwt';

const PLAYER_ROLE = 'player';

export const PLAYER_ONLY_ACCESS_MESSAGE =
  'This app is for player accounts only. Operators and administrators must use the web console.';

export function hasPlayerRole(accessToken: string): boolean {
  return getRealmRoles(accessToken).includes(PLAYER_ROLE);
}

export function assertPlayerRole(accessToken: string): void {
  if (!hasPlayerRole(accessToken)) {
    throw new Error(PLAYER_ONLY_ACCESS_MESSAGE);
  }
}
