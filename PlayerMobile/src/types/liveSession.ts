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
    return 'Your team is already playing in a live session (RN-13). You cannot join another until it ends.';
  }

  const currentRef = input.teamCurrentSessionRef?.trim();
  if (currentRef) {
    const sameSession =
      currentRef.toLowerCase() === input.targetSessionId.trim().toLowerCase();
    if (!sameSession) {
      return 'Your team is already registered in another session. Wait for it to finish before joining a different one.';
    }
  }

  if (!canRequestSessionJoin(input.sessionStatus)) {
    return 'This session is no longer accepting new teams.';
  }

  return null;
}
