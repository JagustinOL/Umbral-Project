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
