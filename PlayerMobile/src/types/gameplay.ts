export type TeamCurrentStage = {
  sessionId: string;
  teamId: string;
  currentNodeId: string | null;
  currentNodeType: string | null;
  currentExecutionOrder: number | null;
  isCompleted: boolean;
};

export type SubmissionResult = {
  isCorrect: boolean;
  currentNodeId: string;
  nextNodeId: string | null;
  awardedPoints: number;
};

export type TeamHint = {
  hintId: string;
  missionNodeId: string;
  order: number;
  content: string;
  penaltyPoints: number;
  releasedAtUtc: string;
  wasManualRelease: boolean;
};

export type RankingEntry = {
  position: number;
  teamId: string;
  teamName: string;
  totalScore: number;
  completedNodes: number;
  lastElapsedSeconds: number;
};

export type TeamPenalty = {
  entryId: string;
  penaltyPoints: number;
  reason: string;
  category: string;
  appliedAtUtc: string;
};

export type TeamFinalSummary = {
  sessionStatus: string;
  sessionId: string;
  teamId: string;
  cancellationReason: string | null;
  teamName: string | null;
  totalScore: number | null;
  rankingPosition: number | null;
  completedNodes: number | null;
  penaltiesApplied: number | null;
  totalElapsedSeconds: number | null;
  startedAtUtc: string | null;
  finalizedAtUtc: string | null;
};

export type SubmittedEvidence = {
  evidenceId: string;
  status: string;
};

export type LiveSessionStatus =
  | 'Pending'
  | 'Preparation'
  | 'Active'
  | 'Paused'
  | 'Finalized'
  | 'Cancelled'
  | string;

export type GameplayTab = 'play' | 'hints' | 'ranking' | 'penalties' | 'summary';

export function normalizeSessionStatus(status: string): LiveSessionStatus {
  return status.trim();
}

export function isSessionPlayable(status: LiveSessionStatus): boolean {
  const n = status.toLowerCase();
  return n === 'active';
}

export function isSessionPaused(status: LiveSessionStatus): boolean {
  return status.toLowerCase() === 'paused';
}

export function isSessionTerminal(status: LiveSessionStatus): boolean {
  const n = status.toLowerCase();
  return n === 'finalized' || n === 'cancelled';
}
