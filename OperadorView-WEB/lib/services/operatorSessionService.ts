import { ApiError, sessionApiRequest } from "@/lib/api/client";
import {
  CreatedLiveSessionDto,
  CreateLiveSessionRequest,
  MissionHasOpenSessionsResponse,
  OperatorAssignedMissionDto,
  OperatorOpenSessionDto,
  SessionTeamsDto,
} from "@/lib/types/api";

export const operatorSessionService = {
  async getAssignedMissions(
    operatorId: string,
    signal?: AbortSignal,
  ): Promise<OperatorAssignedMissionDto[]> {
    return sessionApiRequest<OperatorAssignedMissionDto[]>(
      `/operators/${operatorId}/missions`,
      { signal },
    );
  },

  async createSession(
    operatorId: string,
    missionId: string,
    signal?: AbortSignal,
  ): Promise<CreatedLiveSessionDto> {
    const body: CreateLiveSessionRequest = { missionId };
    return sessionApiRequest<CreatedLiveSessionDto>(`/operators/${operatorId}/sessions`, {
      method: "POST",
      body,
      signal,
    });
  },

  async getSessionTeams(
    operatorId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<SessionTeamsDto> {
    return sessionApiRequest<SessionTeamsDto>(
      `/operators/${operatorId}/sessions/${sessionId}/teams`,
      { signal },
    );
  },

  async startSession(
    operatorId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<void> {
    await sessionApiRequest<void>(`/operators/${operatorId}/sessions/${sessionId}/start`, {
      method: "PUT",
      signal,
    });
  },

  async missionHasOpenSessions(
    missionId: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const response = await sessionApiRequest<MissionHasOpenSessionsResponse>(
      `/missions/${missionId}/session-validation/has-open`,
      { signal },
    );
    return response.hasOpenSessions;
  },

  async getOpenSessions(
    operatorId: string,
    signal?: AbortSignal,
  ): Promise<OperatorOpenSessionDto[]> {
    return sessionApiRequest<OperatorOpenSessionDto[]>(
      `/operators/${operatorId}/sessions/open`,
      { signal },
    );
  },
};

export function getOperatorSessionApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Error inesperado al comunicarse con el servidor de sesiones.";
  }

  if (error.status === 404) {
    return "Recurso no encontrado o sin acceso a esta misión.";
  }

  if (error.status === 409) {
    return error.message || "La operación fue bloqueada por las reglas del juego.";
  }

  if (error.status === 400) {
    return error.message || "Los datos enviados no son válidos.";
  }

  return error.message;
}
