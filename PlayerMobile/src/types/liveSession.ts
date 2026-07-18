export type LiveSessionSummary = {
  sessionId: string;
  missionRef: string;
  joinCode: string;
  status: string;
  createdAtUtc: string;
  missionTitle?: string;
};

export function canRequestSessionJoin(status: string): boolean {
  const normalized = status.trim().toLowerCase();
  return normalized === 'pending' || normalized === 'preparation';
}

export function getSessionJoinBlockReason(input: {
  sessionStatus: string;
  teamIsLocked: boolean;
  teamCurrentSessionRef: string | null;
  targetSessionId: string;
}): string | null {
  if (input.teamIsLocked) {
    return 'Tu equipo ya está jugando en una sesión en vivo. No puedes unirte a otra hasta que termine.';
  }

  const currentRef = input.teamCurrentSessionRef?.trim();
  if (currentRef) {
    const sameSession =
      currentRef.toLowerCase() === input.targetSessionId.trim().toLowerCase();
    if (sameSession) {
      return 'Tu equipo ya está registrado en esta sesión.';
    }
    return 'Tu equipo ya está registrado en otra sesión. Espera a que finalice antes de unirte a otra distinta.';
  }

  if (!canRequestSessionJoin(input.sessionStatus)) {
    return 'This session is no longer accepting new teams.';
  }

  return null;
}
