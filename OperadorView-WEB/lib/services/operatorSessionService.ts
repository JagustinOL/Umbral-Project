import { ApiError, scoringApiRequest, sessionApiRequest } from "@/lib/api/client";
import {
  CreatedLiveSessionDto,
  CreateLiveSessionRequest,
  HistoricalSessionsPageDto,
  MissionHasOpenSessionsResponse,
  OperatorAssignedMissionDto,
  OperatorOpenSessionDto,
  OperatorSessionBoardDto,
  RankingEntryDto,
  SessionAuditDetailDto,
  SessionJoinRequestDto,
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

  async getJoinRequests(sessionId: string, signal?: AbortSignal): Promise<SessionJoinRequestDto[]> {
    return sessionApiRequest<SessionJoinRequestDto[]>(`/sessions/${sessionId}/join-requests`, { signal });
  },

  async decideJoinRequest(
    sessionId: string,
    teamId: string,
    decision: "Approve" | "Reject",
    signal?: AbortSignal,
  ): Promise<void> {
    await sessionApiRequest<void>(`/sessions/${sessionId}/join-requests/${teamId}/decision`, {
      method: "POST",
      body: { decision },
      signal,
    });
  },

  async getOperatorBoard(
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<OperatorSessionBoardDto> {
    return sessionApiRequest<OperatorSessionBoardDto>(`/sessions/${sessionId}/operator-board`, {
      signal,
    });
  },

  async releaseHint(sessionId: string, teamId: string, hintId: string): Promise<void> {
    await sessionApiRequest<void>(`/sessions/${sessionId}/teams/${teamId}/hints/release`, {
      method: "POST",
      body: { hintId },
    });
  },

  async applyPenalty(sessionId: string, teamId: string, points: number, reason: string): Promise<void> {
    await sessionApiRequest<void>(`/sessions/${sessionId}/teams/${teamId}/penalties`, {
      method: "POST",
      body: { points, reason },
    });
  },

  async sendMessage(sessionId: string, teamId: string, message: string): Promise<void> {
    await sessionApiRequest<void>(`/sessions/${sessionId}/teams/${teamId}/messages`, {
      method: "POST",
      body: { message },
    });
  },

  async togglePause(sessionId: string): Promise<{ status: string }> {
    return sessionApiRequest<{ status: string }>(`/sessions/${sessionId}/pause`, { method: "POST" });
  },

  async finalizeSession(operatorId: string, sessionId: string): Promise<void> {
    await sessionApiRequest<void>(`/operators/${operatorId}/sessions/${sessionId}/finalize`, {
      method: "PUT",
    });
  },

  async cancelSession(operatorId: string, sessionId: string): Promise<void> {
    await sessionApiRequest<void>(`/operators/${operatorId}/sessions/${sessionId}/cancel`, {
      method: "PUT",
    });
  },

  async getRanking(sessionId: string, signal?: AbortSignal): Promise<RankingEntryDto[]> {
    return scoringApiRequest<RankingEntryDto[]>(`/sessions/${sessionId}/ranking`, { signal });
  },

  async getHistoricalSessions(signal?: AbortSignal): Promise<HistoricalSessionsPageDto> {
    return scoringApiRequest<HistoricalSessionsPageDto>("/audit/sessions", { signal });
  },

  async getSessionAuditDetail(sessionId: string, signal?: AbortSignal): Promise<SessionAuditDetailDto> {
    return scoringApiRequest<SessionAuditDetailDto>(`/audit/sessions/${sessionId}`, { signal });
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
