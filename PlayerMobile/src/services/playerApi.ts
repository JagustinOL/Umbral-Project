import { API_CONFIG, API_PATHS } from '../constants/api';
import type { PlayerProfile } from '../types/auth';
import { normalizeGuid } from '../utils/uuid';
import { apiRequest } from './apiClient';

type CreatePlayerResponse = { id: string };

type PlayerApiDto = {
  playerId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
};

function playerPath(playerId: string, suffix = ''): string {
  return `${API_PATHS.players}/${normalizeGuid(playerId)}${suffix}`;
}

function mapPlayer(dto: PlayerApiDto): PlayerProfile {
  return {
    playerId: dto.playerId,
    firstName: dto.firstName,
    lastName: dto.lastName,
    email: dto.email,
    isActive: dto.isActive,
  };
}

export async function createPlayer(input: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}): Promise<string> {
  const response = await apiRequest<CreatePlayerResponse>(API_PATHS.players, {
    method: 'POST',
    baseUrl: API_CONFIG.missionManagementBaseUrl,
    body: input,
    skipAuth: true,
  });

  return response.id;
}

export async function getPlayers(): Promise<PlayerProfile[]> {
  const response = await apiRequest<PlayerApiDto[]>(API_PATHS.players, {
    baseUrl: API_CONFIG.missionManagementBaseUrl,
  });

  return response.map(mapPlayer);
}

export async function getPlayerById(playerId: string): Promise<PlayerProfile> {
  const response = await apiRequest<PlayerApiDto>(playerPath(playerId), {
    baseUrl: API_CONFIG.missionManagementBaseUrl,
  });

  return mapPlayer(response);
}

export async function updatePlayer(
  playerId: string,
  input: {
    firstName: string;
    lastName: string;
    email: string;
  },
): Promise<void> {
  await apiRequest<void>(playerPath(playerId), {
    method: 'PUT',
    baseUrl: API_CONFIG.missionManagementBaseUrl,
    body: {
      firstName: input.firstName.trim(),
      lastName: input.lastName.trim(),
      email: input.email.trim(),
    },
  });
}

export async function deactivatePlayer(playerId: string): Promise<void> {
  await apiRequest<void>(playerPath(playerId, '/deactivate'), {
    method: 'PUT',
    baseUrl: API_CONFIG.missionManagementBaseUrl,
    body: {},
  });
}
