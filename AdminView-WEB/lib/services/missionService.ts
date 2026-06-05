import { Mission } from "@/lib/types";
import { ApiError, apiRequest } from "@/lib/api/client";
import {
  AssignOperatorToMissionCommand,
  CreateMissionRequest,
  CreateMissionResponse,
  MissionDifficulty,
  MissionDto,
  MissionHasOpenSessionsResponse,
  UpdateMissionDetailsRequest,
} from "@/lib/types/api";

const difficultyToNumber: Record<MissionDifficulty, number> = {
  Easy: 1,
  Medium: 2,
  Hard: 3,
};

const difficultyToApi: Record<number, 1 | 2 | 3> = {
  1: 1,
  2: 2,
  3: 3,
};

export function toMissionViewModel(dto: MissionDto): Mission {
  return {
    id: dto.id,
    title: dto.title,
    description: dto.description,
    status: dto.status,
    difficulty: difficultyToNumber[dto.difficulty] ?? 1,
    maxDurationMinutes: dto.maxDurationMinutes,
    nodes: [],
    assignedOperators: (dto.operatorIds ?? []).map((operatorId) => ({ operatorId })),
  };
}

export const missionService = {
  async getMissions(signal?: AbortSignal): Promise<MissionDto[]> {
    return apiRequest<MissionDto[]>("/missions", { signal });
  },

  async createMission(payload: {
    title: string;
    description: string;
    difficulty: number;
    maxDurationMinutes?: number;
  }): Promise<CreateMissionResponse> {
    const request: CreateMissionRequest = {
      title: payload.title,
      description: payload.description,
      difficulty: difficultyToApi[payload.difficulty] ?? 1,
      maxDurationMinutes: payload.maxDurationMinutes,
    };

    return apiRequest<CreateMissionResponse>("/missions", {
      method: "POST",
      body: request,
    });
  },

  async updateMission(
    missionId: string,
    payload: {
      title: string;
      description: string;
      maxDurationMinutes?: number;
    },
  ): Promise<void> {
    const request: UpdateMissionDetailsRequest = {
      title: payload.title,
      description: payload.description,
      maxDurationMinutes: payload.maxDurationMinutes,
    };

    await apiRequest<void>(`/missions/${missionId}`, {
      method: "PUT",
      body: request,
    });
  },

  async deleteMission(missionId: string): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}`, {
      method: "DELETE",
    });
  },

  async missionHasOpenSessions(missionId: string): Promise<boolean> {
    const response = await apiRequest<MissionHasOpenSessionsResponse>(
      `/missions/${missionId}/session-validation/has-open`,
    );
    return response.hasOpenSessions;
  },

  async assignOperatorToMission(missionId: string, operatorId: string): Promise<void> {
    const command: AssignOperatorToMissionCommand = { operatorId };
    await apiRequest<void>(`/missions/${missionId}/operators`, {
      method: "POST",
      body: command,
    });
  },

  async revokeOperatorFromMission(missionId: string, operatorId: string): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/operators/${operatorId}`, {
      method: "DELETE",
    });
  },
};

export function getMissionApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Unexpected error while communicating with Mission API.";
  }

  if (error.status === 409) {
    return "Mission update/deactivation was blocked by business rules (RN-01). The mission may have open sessions or conflicting data.";
  }

  if (error.status === 400) {
    return "Mission payload failed business validation. Please review the form data.";
  }

  if (error.status === 404) {
    return "Mission was not found. Refresh the catalog and try again.";
  }

  return error.message;
}

export function logMissionOperatorApiProblem(error: unknown): void {
  if (error instanceof ApiError && (error.status === 400 || error.status === 409)) {
    console.error("Mission operator API ProblemDetails:", error.details);
  }
}

export function getMissionOperatorAssignmentErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Error inesperado al gestionar la asignación de operadores.";
  }

  if (error.status === 409) {
    return "Revocación bloqueada (RN-25): el operador supervisa una sesión activa de esta misión.";
  }

  if (error.status === 400) {
    return "El operador ya está asignado a esta misión o los datos no son válidos.";
  }

  if (error.status === 404) {
    return "La misión u operador no fue encontrado. Actualiza el listado e intenta de nuevo.";
  }

  return getMissionApiErrorMessage(error);
}
