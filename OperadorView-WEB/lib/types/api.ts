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
