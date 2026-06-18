import { Hint } from "@/lib/types";
import { ApiError, apiRequest } from "@/lib/api/client";
import { AddHintResponse, HintDto, UpdateHintCommand } from "@/lib/types/api";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

export function toHintViewModel(dto: HintDto, nodeId: string): Hint {
  return {
    id: dto.id,
    nodeId,
    content: dto.content,
    order: dto.order,
    penaltyPoints: dto.penaltyPoints,
  };
}

export function sortHintsByOrder(hints: Hint[]): Hint[] {
  return [...hints].sort((a, b) => a.order - b.order);
}

export function validateHintContent(content: string): string | null {
  if (!content.trim()) {
    return "El contenido de la pista es obligatorio.";
  }
  return null;
}

export const hintService = {
  async getHintsByNode(
    missionId: string,
    nodeId: string,
    signal?: AbortSignal,
  ): Promise<HintDto[]> {
    return apiRequest<HintDto[]>(`/missions/${missionId}/nodes/${nodeId}/hints`, { signal });
  },

  async addHint(missionId: string, nodeId: string, content: string): Promise<AddHintResponse> {
    return apiRequest<AddHintResponse>(`/missions/${missionId}/nodes/${nodeId}/hints`, {
      method: "POST",
      body: { content },
    });
  },

  async updateHint(
    missionId: string,
    nodeId: string,
    hintId: string,
    command: UpdateHintCommand,
  ): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}/hints/${hintId}`, {
      method: "PUT",
      body: command,
    });
  },

  async deleteHint(missionId: string, nodeId: string, hintId: string): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}/hints/${hintId}`, {
      method: "DELETE",
    });
  },
};

export function logHintApiProblem(error: unknown): void {
  if (error instanceof ApiError && (error.status === 400 || error.status === 409)) {
    console.error("Hint API ProblemDetails:", error.details);
  }
}

export function getHintApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Error inesperado al comunicarse con la API de pistas.";
  }

  if (isRecord(error.details)) {
    const detail = error.details.detail;
    if (typeof detail === "string" && detail.length > 0) {
      return detail;
    }
    const title = error.details.title;
    if (typeof title === "string" && title.length > 0) {
      return title;
    }
  }

  if (error.status === 409) {
    return "Operación bloqueada por RN-01.";
  }

  if (error.status === 400) {
    return "Los datos de la pista no pasaron la validación.";
  }

  if (error.status === 404) {
    return "La pista o el nodo no fue encontrado.";
  }

  return error.message;
}
