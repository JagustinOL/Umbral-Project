import { API_CONFIG, API_PATHS } from '../constants/api';
import type { JoinRequest, TeamDetails, TeamMember } from '../types/team';
import { normalizeGuid } from '../utils/uuid';
import { isValidTeamCode, normalizeTeamCode } from '../utils/validation';
import { apiRequest } from './apiClient';

function teamPath(teamId: string, suffix = ''): string {
  return `${API_PATHS.teams}/${normalizeGuid(teamId)}${suffix}`;
}

type TeamDetailsApiDto = {
  teamId: string;
  name: string;
  teamCode: string;
  isLocked: boolean;
  currentSessionRef?: string | null;
  isDisbanded: boolean;
  members: TeamMember[];
};

type SubmitJoinResponse = {
  requestId: string;
  teamId: string;
};

export type PlayerTeamMembership = {
  isMember: boolean;
  teamId: string | null;
  teamName: string | null;
  teamCode: string | null;
  role: string | null;
  hasPendingJoinRequest: boolean;
  pendingTeamId: string | null;
};

type PlayerTeamMembershipApiDto = {
  isMember: boolean;
  teamId: string | null;
  teamName: string | null;
  teamCode: string | null;
  role: string | null;
  hasPendingJoinRequest: boolean;
  pendingTeamId: string | null;
};

function mapPlayerTeamMembership(dto: PlayerTeamMembershipApiDto): PlayerTeamMembership {
  return {
    isMember: dto.isMember,
    teamId: dto.teamId,
    teamName: dto.teamName,
    teamCode: dto.teamCode,
    role: dto.role,
    hasPendingJoinRequest: dto.hasPendingJoinRequest,
    pendingTeamId: dto.pendingTeamId,
  };
}

export async function getPlayerTeamMembership(
  playerId: string,
): Promise<PlayerTeamMembership> {
  const dto = await apiRequest<PlayerTeamMembershipApiDto>(
    API_PATHS.playerTeamMembership(playerId),
    { baseUrl: API_CONFIG.sessionManagementBaseUrl },
  );

  return mapPlayerTeamMembership(dto);
}

function mapTeam(dto: TeamDetailsApiDto): TeamDetails {
  return {
    teamId: dto.teamId,
    name: dto.name,
    teamCode: dto.teamCode,
    isLocked: dto.isLocked,
    currentSessionRef: dto.currentSessionRef ?? null,
    isDisbanded: dto.isDisbanded,
    members: dto.members,
    pendingRequests: [],
  };
}

export async function createTeam(input: {
  name: string;
  creatorId: string;
  creatorDisplayName: string;
}): Promise<string> {
  const response = await apiRequest<{ id: string }>(API_PATHS.teams, {
    method: 'POST',
    baseUrl: API_CONFIG.sessionManagementBaseUrl,
    body: {
      name: input.name,
      creatorId: input.creatorId,
      creatorDisplayName: input.creatorDisplayName,
    },
  });

  return response.id;
}

export async function getTeamById(teamId: string): Promise<TeamDetails> {
  const team = await apiRequest<TeamDetailsApiDto>(
    teamPath(teamId),
    {
      baseUrl: API_CONFIG.sessionManagementBaseUrl,
    },
  );

  return mapTeam(team);
}

export async function submitJoinRequest(input: {
  teamCode: string;
  playerRef: string;
  displayName: string;
}): Promise<SubmitJoinResponse> {
  const code = normalizeTeamCode(input.teamCode);
  if (!isValidTeamCode(code)) {
    throw new Error('Team code must be exactly 6 alphanumeric characters.');
  }

  return apiRequest<SubmitJoinResponse>(API_PATHS.joinRequests, {
    method: 'POST',
    baseUrl: API_CONFIG.sessionManagementBaseUrl,
    body: {
      teamCode: code,
      playerRef: input.playerRef,
      displayName: input.displayName,
    },
  });
}

export async function getPendingRequests(
  teamId: string,
  requestorId: string,
): Promise<JoinRequest[]> {
  const requests = await apiRequest<
    Array<{
      requestId: string;
      playerRef: string;
      displayName: string;
      status: string;
      requestedAtUtc: string;
      reviewedAtUtc?: string | null;
    }>
  >(`${teamPath(teamId)}/requests?requestorId=${normalizeGuid(requestorId)}`, {
    baseUrl: API_CONFIG.sessionManagementBaseUrl,
  });

  return requests.map((entry) => ({
    requestId: entry.requestId,
    playerRef: entry.playerRef,
    displayName: entry.displayName,
    status: entry.status as JoinRequest['status'],
    requestedAtUtc: entry.requestedAtUtc,
  }));
}

export async function processJoinRequest(input: {
  teamId: string;
  requestId: string;
  approve: boolean;
  requestorId: string;
}): Promise<void> {
  await apiRequest<void>(
    `${teamPath(input.teamId)}/requests/${normalizeGuid(input.requestId)}`,
    {
      method: 'PUT',
      baseUrl: API_CONFIG.sessionManagementBaseUrl,
      body: {
        approve: input.approve,
        requestorId: normalizeGuid(input.requestorId),
      },
    },
  );
}

export async function updateTeamName(input: {
  teamId: string;
  newName: string;
  requestorId: string;
}): Promise<void> {
  await apiRequest<void>(teamPath(input.teamId), {
    method: 'PUT',
    baseUrl: API_CONFIG.sessionManagementBaseUrl,
    body: {
      newName: input.newName,
      requestorId: normalizeGuid(input.requestorId),
    },
  });
}

export async function disbandTeam(
  teamId: string,
  requestorId: string,
): Promise<void> {
  await apiRequest<void>(
    `${teamPath(teamId)}?requestorId=${normalizeGuid(requestorId)}`,
    {
      method: 'DELETE',
      baseUrl: API_CONFIG.sessionManagementBaseUrl,
    },
  );
}

export async function removeMember(input: {
  teamId: string;
  playerId: string;
  requestorId: string;
}): Promise<void> {
  const player = normalizeGuid(input.playerId);
  const requestor = normalizeGuid(input.requestorId);
  await apiRequest<void>(
    `${teamPath(input.teamId)}/members/${player}?requestorId=${requestor}`,
    {
      method: 'DELETE',
      baseUrl: API_CONFIG.sessionManagementBaseUrl,
    },
  );
}

export async function leaveTeam(
  teamId: string,
  playerId: string,
): Promise<void> {
  const player = normalizeGuid(playerId);
  await removeMember({
    teamId,
    playerId: player,
    requestorId: player,
  });
}
