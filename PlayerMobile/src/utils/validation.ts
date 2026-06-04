const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const TEAM_CODE_PATTERN = /^[A-Z0-9]{6}$/;

export function isValidEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value.trim());
}

export function isPasswordMinLength(value: string, min = 8): boolean {
  return value.length >= min;
}

export function isNonEmpty(value: string): boolean {
  return value.trim().length > 0;
}

export function normalizeTeamCode(value: string): string {
  return value.trim().toUpperCase().replace(/[^A-Z0-9]/g, '');
}

export function isValidTeamCode(value: string): boolean {
  return TEAM_CODE_PATTERN.test(normalizeTeamCode(value));
}
