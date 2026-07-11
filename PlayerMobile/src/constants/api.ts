export const API_CONFIG = {
  userServiceBaseUrl: (
    process.env.EXPO_PUBLIC_USER_API_URL?.trim() || 'http://localhost:5200'
  ).replace(/\/+$/, ''),
  missionManagementBaseUrl: (
    process.env.EXPO_PUBLIC_MISSION_API_URL?.trim() || 'http://localhost:5200'
  ).replace(/\/+$/, ''),
  sessionManagementBaseUrl: (
    process.env.EXPO_PUBLIC_SESSION_API_URL?.trim() || 'http://localhost:5200'
  ).replace(/\/+$/, ''),
  scoringAuditBaseUrl: (
    process.env.EXPO_PUBLIC_SCORING_API_URL?.trim() || 'http://localhost:5200'
  ).replace(/\/+$/, ''),
} as const;

export const API_PATHS = {
  players: '/api/v1/players',
  missions: '/api/v1/missions',
  teams: '/api/v1/teams',
  joinRequests: '/api/v1/teams/join-requests',
  playerTeamMembership: (playerId: string) =>
    `/api/v1/players/${playerId}/team-membership`,
  liveSessionsActive: '/api/v1/live-sessions/active',
  liveSessionsJoin: '/api/v1/live-sessions/join',
  teamCurrentStage: (sessionId: string, teamId: string) =>
    `/api/v1/live-sessions/${sessionId}/teams/${teamId}/current-stage`,
  teamCurrentNodeContent: (sessionId: string, teamId: string) =>
    `/api/v1/live-sessions/${sessionId}/teams/${teamId}/current-node-content`,
  submitTrivia: (sessionId: string, teamId: string) =>
    `/api/v1/live-sessions/${sessionId}/teams/${teamId}/trivia-answer`,
  submitTreasure: (sessionId: string, teamId: string) =>
    `/api/v1/live-sessions/${sessionId}/teams/${teamId}/treasure-hunt-code`,
  submitEvidence: (sessionId: string, teamId: string) =>
    `/api/v1/sessions/${sessionId}/teams/${teamId}/evidences`,
  teamHints: (sessionId: string, teamId: string) =>
    `/api/v1/sessions/${sessionId}/teams/${teamId}/hints`,
  teamPenalties: (sessionId: string, teamId: string) =>
    `/api/v1/sessions/${sessionId}/teams/${teamId}/penalties`,
  teamSummary: (sessionId: string, teamId: string) =>
    `/api/v1/sessions/${sessionId}/teams/${teamId}/summary`,
  sessionRanking: (sessionId: string) =>
    `/api/v1/sessions/${sessionId}/ranking`,
  signalRHub: '/hubs/live-session',
} as const;

export const MAX_TEAM_MEMBERS = 4;
export const TEAM_CODE_LENGTH = 6;

export const DOMAIN_ERRORS = {
  teamLocked:
    'RN-13: Team modifications are disabled while an active or paused session is in progress.',
  duplicateTeamName:
    'RN-14: A team with this name already exists. Choose a unique team name.',
  emptyTeamName: 'Team name cannot be empty.',
  invalidTeamCode: 'Team code must be exactly 6 alphanumeric characters.',
  teamNotFound: 'No team found for the provided code.',
  notLeader: 'Only the team leader can perform this action.',
} as const;
