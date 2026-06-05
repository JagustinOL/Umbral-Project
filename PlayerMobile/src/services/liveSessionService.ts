import { API_CONFIG, API_PATHS } from '../constants/api';
import type { LiveSessionSummary } from '../types/liveSession';
import { normalizeGuid } from '../utils/uuid';
import { apiRequest } from './apiClient';
import { getMissionById } from './missionApi';

type ActiveSessionApiDto = {
  sessionId: string;
  missionRef: string;
  joinCode: string;
  status: string;
  createdAtUtc: string;
};

function mapSession(dto: ActiveSessionApiDto): LiveSessionSummary {
  return {
    sessionId: dto.sessionId,
    missionRef: dto.missionRef,
    joinCode: dto.joinCode,
    status: dto.status,
    createdAtUtc: dto.createdAtUtc,
  };
}

export async function getActiveSessions(): Promise<LiveSessionSummary[]> {
  const sessions = await apiRequest<ActiveSessionApiDto[]>(
    API_PATHS.liveSessionsActive,
    { baseUrl: API_CONFIG.sessionManagementBaseUrl },
  );

  const mapped = sessions.map(mapSession);
  const missionTitles = new Map<string, string>();

  await Promise.all(
    mapped.map(async (session) => {
      const missionId = normalizeGuid(session.missionRef);
      if (missionTitles.has(missionId)) {
        session.missionTitle = missionTitles.get(missionId);
        return;
      }

      try {
        const mission = await getMissionById(missionId);
        missionTitles.set(missionId, mission.title);
        session.missionTitle = mission.title;
      } catch {
        session.missionTitle = `Mission ${missionId.slice(0, 8)}…`;
      }
    }),
  );

  return mapped;
}

export async function requestSessionJoin(input: {
  joinCode: string;
  teamId: string;
}): Promise<string> {
  const response = await apiRequest<{ sessionId: string }>(
    API_PATHS.liveSessionsJoin,
    {
      method: 'POST',
      baseUrl: API_CONFIG.sessionManagementBaseUrl,
      body: {
        joinCode: input.joinCode.trim(),
        teamId: normalizeGuid(input.teamId),
      },
    },
  );

  return response.sessionId;
}
