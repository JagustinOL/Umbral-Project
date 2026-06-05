import { MissionNode } from "@/lib/types";
import { ApiError, apiRequest } from "@/lib/api/client";
import {
  AddRootNodeCommand,
  AddRootNodeResponse,
  MissionNodeDto,
  MissionNodeType,
  MissionStatus,
  StageGameDto,
  UpdateNodeCommand,
} from "@/lib/types/api";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const parseNodeType = (nodeType: string): MissionNodeType => {
  if (nodeType === "Stage" || nodeType === "Trivia" || nodeType === "TreasureHunt") {
    return nodeType;
  }
  return "Stage";
};

export function toStageViewModel(dto: MissionNodeDto, missionId: string): MissionNode {
  return {
    id: dto.id,
    missionId,
    parentNodeId: dto.parentNodeId ?? undefined,
    type: parseNodeType(dto.nodeType),
    title: dto.title,
    description: dto.description,
    executionOrder: dto.executionOrder,
    children: [],
    hints: [],
  };
}

export function sortStagesByExecutionOrder(stages: MissionNode[]): MissionNode[] {
  return [...stages].sort((a, b) => a.executionOrder - b.executionOrder);
}

export function mergeStageNodesWithLocalChildren(
  apiStages: MissionNode[],
  existingNodes: MissionNode[],
): MissionNode[] {
  const childByStageId = new Map(
    existingNodes
      .filter((n) => n.type === "Stage")
      .map((stage) => [stage.id, stage.children ?? []] as const),
  );

  return apiStages.map((stage) => ({
    ...stage,
    children: childByStageId.get(stage.id) ?? [],
  }));
}

export const nodeService = {
  async getNodesByMission(missionId: string, signal?: AbortSignal): Promise<MissionNodeDto[]> {
    return apiRequest<MissionNodeDto[]>(`/missions/${missionId}/nodes`, { signal });
  },

  async getGamesByStage(
    missionId: string,
    stageId: string,
    signal?: AbortSignal,
  ): Promise<StageGameDto[]> {
    return apiRequest<StageGameDto[]>(`/missions/${missionId}/nodes/${stageId}/games`, { signal });
  },

  async addRootNode(
    missionId: string,
    command: AddRootNodeCommand,
  ): Promise<AddRootNodeResponse> {
    return apiRequest<AddRootNodeResponse>(`/missions/${missionId}/nodes`, {
      method: "POST",
      body: command,
    });
  },

  async updateNode(
    missionId: string,
    nodeId: string,
    command: UpdateNodeCommand,
  ): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}`, {
      method: "PUT",
      body: command,
    });
  },

  async deleteNode(missionId: string, nodeId: string): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}`, {
      method: "DELETE",
    });
  },
};

export function logNodeApiProblem(error: unknown): void {
  if (error instanceof ApiError && (error.status === 400 || error.status === 409)) {
    console.error("Node API ProblemDetails:", error.details);
  }
}

export function getNodeApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Error inesperado al comunicarse con la API de etapas.";
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
    return "Operación bloqueada por reglas de negocio (RN-01). La estructura no se puede modificar.";
  }

  if (error.status === 400) {
    return "Los datos de la etapa no pasaron la validación. Revisa el formulario.";
  }

  if (error.status === 404) {
    return "La misión o la etapa no fue encontrada. Actualiza el catálogo e intenta de nuevo.";
  }

  return error.message;
}

export function getStructureLockTooltip(status: MissionStatus): string {
  if (status === "Active") {
    return "Estructura inmutable: La misión está activa";
  }
  if (status === "Inactive") {
    return "Estructura inmutable: La misión no está en borrador";
  }
  return "";
}
