import { API_CONFIG, API_PATHS } from '../constants/api';
import { normalizeGuid } from '../utils/uuid';
import { apiRequest } from './apiClient';

type MissionApiDto = {
  id: string;
  title: string;
  description: string;
  status: string;
};

export type MissionSummary = {
  id: string;
  title: string;
  description: string;
  status: string;
};

function mapMission(dto: MissionApiDto): MissionSummary {
  return {
    id: dto.id,
    title: dto.title,
    description: dto.description,
    status: dto.status,
  };
}

export async function getMissionById(missionId: string): Promise<MissionSummary> {
  const dto = await apiRequest<MissionApiDto>(
    `${API_PATHS.missions}/${normalizeGuid(missionId)}`,
    { baseUrl: API_CONFIG.missionManagementBaseUrl },
  );

  return mapMission(dto);
}
