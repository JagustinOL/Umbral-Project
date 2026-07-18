export interface OperatorAssignedMissionDto {
  missionId: string;
  operatorId: string;
  title: string;
}

export interface CreateLiveSessionRequest {
  missionId: string;
}

export interface CreatedLiveSessionDto {
  sessionId: string;
  joinCode: string;
}

export interface SessionTeamsDto {
  sessionId: string;
  teamIds: string[];
  teamCount: number;
}

export interface MissionHasOpenSessionsResponse {
  hasOpenSessions: boolean;
}

export interface OperatorOpenSessionDto {
  sessionId: string;
  missionId: string;
  joinCode: string;
  status: string;
}

export interface OperatorDto {
  operatorId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

export interface SessionJoinRequestDto {
  requestId: string;
  teamId: string;
  teamName: string | null;
  status: string;
  requestedAtUtc: string;
  resolvedAtUtc: string | null;
}

export interface RankingEntryDto {
  position?: number;
  teamId: string;
  teamName: string;
  totalScore: number;
  completedNodes: number;
  lastElapsedSeconds: number;
}

export interface OperatorAvailableHintDto {
  hintId: string;
  order: number;
  content: string;
  penaltyPoints: number;
  nodeType: string | null;
  nodePrompt: string | null;
}

export interface OperatorReleasedHintSummaryDto {
  hintId: string;
  missionNodeId: string;
  penaltyPoints: number;
  releasedAtUtc: string;
  wasManualRelease: boolean;
  nodeType: string | null;
  nodePrompt: string | null;
}

export interface OperatorTeamBoardEntryDto {
  teamId: string;
  teamName: string | null;
  participationStatus: string;
  currentNodeId: string | null;
  currentNodeType: string | null;
  currentExecutionOrder: number | null;
  currentGameLabel: string | null;
  isMissionCompleted: boolean;
  availableHints: OperatorAvailableHintDto[];
  releasedHints: OperatorReleasedHintSummaryDto[];
}

export interface OperatorSessionBoardDto {
  sessionId: string;
  sessionStatus: string;
  startedAtUtc: string | null;
  teams: OperatorTeamBoardEntryDto[];
}

export interface HistoricalSessionDto {
  sessionId: string;
  missionId: string;
  operatorId: string;
  startedAtUtc: string;
  endedAtUtc: string | null;
  status: string;
}

export interface HistoricalSessionsPageDto {
  items: HistoricalSessionDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditDashboardDto {
  generalRanking: {
    position: number;
    teamId: string;
    teamName: string;
    totalScore: number;
    elapsedSeconds: number;
    sessionId: string;
    missionId: string;
  }[];
  topMissions: { missionId: string; sessionCount: number }[];
  topOperators: { operatorId: string; sessionCount: number }[];
  totalFinishedSessions: number;
}

export interface SessionAuditDetailDto {
  sessionId: string;
  missionId: string;
  operatorId: string;
  startedAtUtc: string;
  endedAtUtc: string | null;
  status: string;
  timeline: SessionAuditEventDto[];
  penalties: SessionAuditPenaltyDto[];
  evidences: SessionAuditEvidenceDto[];
  ranking: RankingEntryDto[];
}

export interface SessionAuditEventDto {
  eventId: string;
  eventType: string;
  occurredAtUtc: string;
  description: string;
  teamId: string | null;
  missionNodeId: string | null;
  metadata: string | null;
}

export interface SessionAuditPenaltyDto {
  teamId: string;
  points: number;
  category: string;
  description: string;
  appliedByOperatorId: string | null;
  recordedAtUtc: string;
}

export interface SessionAuditEvidenceDto {
  teamId: string;
  missionNodeId: string;
  points: number;
  elapsedSeconds: number;
  recordedAtUtc: string;
}
