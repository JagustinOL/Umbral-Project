export function normalizeGuid(value: string): string {
  return value.trim().toLowerCase();
}

export function samePlayerRef(a: string, b: string): boolean {
  return normalizeGuid(a) === normalizeGuid(b);
}

export function isTeamLeaderRole(role: string | null | undefined): boolean {
  return role?.trim().toLowerCase() === 'leader';
}

export function createId(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const random = (Math.random() * 16) | 0;
    const value = char === 'x' ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}
