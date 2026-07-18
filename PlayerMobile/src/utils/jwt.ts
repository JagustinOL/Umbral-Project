export type JwtPayload = {
  sub?: string;
  email?: string;
  preferred_username?: string;
  given_name?: string;
  family_name?: string;
  exp?: number;
  realm_access?: {
    roles?: string[];
  };
};

export function getRealmRoles(token: string): string[] {
  const payload = decodeJwtPayload(token);
  if (!payload?.realm_access?.roles) {
    return [];
  }

  return payload.realm_access.roles
    .map((role) => role.trim().toLowerCase())
    .filter((role) => role.length > 0);
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split('.');
  if (parts.length < 2) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const json = atob(padded);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}
