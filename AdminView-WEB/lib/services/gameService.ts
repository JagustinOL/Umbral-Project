import { GpsCoordinate, MissionNode, TriviaQuestion } from "@/lib/types";
import { ApiError, apiRequest } from "@/lib/api/client";
import {
  AddTreasureHuntNodeCommand,
  AddTreasureHuntNodeResponse,
  AddTriviaNodeCommand,
  AddTriviaNodeResponse,
  StageGameDto,
  TriviaNodeDto,
  TriviaQuestionDto,
  TriviaQuestionPayload,
  TreasureHuntNodeDto,
  UpdateTreasureHuntNodeCommand,
  UpdateTriviaNodeCommand,
} from "@/lib/types/api";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

export function toUiTriviaQuestions(dtos: TriviaQuestionDto[]): TriviaQuestion[] {
  return dtos.map((q, questionIndex) => ({
    id: `q-${questionIndex}`,
    questionText: q.prompt,
    options: q.options.map((text, optionIndex) => ({
      id: `o-${questionIndex}-${optionIndex}`,
      text,
      isCorrect: optionIndex === q.correctOptionIndex,
    })),
  }));
}

export function toApiTriviaQuestions(questions: TriviaQuestion[]): TriviaQuestionPayload[] {
  return questions.map((q) => {
    const correctOptionIndex = q.options.findIndex((o) => o.isCorrect);
    return {
      prompt: q.questionText.trim(),
      options: q.options.map((o) => o.text.trim()),
      correctOptionIndex: correctOptionIndex >= 0 ? correctOptionIndex : 0,
    };
  });
}

export function validateTriviaQuestions(questions: TriviaQuestion[]): string | null {
  if (questions.length === 0) {
    return "La trivia debe tener al menos una pregunta con una respuesta correcta.";
  }

  for (const [index, question] of questions.entries()) {
    if (!question.questionText.trim()) {
      return `La pregunta ${index + 1} debe tener texto.`;
    }
    if (question.options.length < 2) {
      return `La pregunta ${index + 1} debe tener al menos dos opciones.`;
    }
    const correctCount = question.options.filter((o) => o.isCorrect).length;
    if (correctCount !== 1) {
      return `La pregunta ${index + 1} debe tener exactamente una respuesta correcta.`;
    }
    if (question.options.some((o) => !o.text.trim())) {
      return `Todas las opciones de la pregunta ${index + 1} deben tener texto.`;
    }
  }

  return null;
}

export function validateTreasureHuntPayload(payload: {
  instructions: string;
  secretCode: string;
  destination: GpsCoordinate;
}): string | null {
  if (!payload.instructions.trim()) {
    return "Las instrucciones son obligatorias.";
  }
  if (!payload.secretCode.trim()) {
    return "El código secreto es obligatorio.";
  }
  if (
    Number.isNaN(payload.destination.latitude) ||
    Number.isNaN(payload.destination.longitude)
  ) {
    return "Las coordenadas GPS deben ser válidas.";
  }
  return null;
}

export function validateBaseScore(baseScore: number): string | null {
  if (!Number.isFinite(baseScore) || !Number.isInteger(baseScore) || baseScore <= 0) {
    return "El puntaje base del juego debe ser un entero mayor que cero.";
  }
  return null;
}

export function toTriviaGameViewModel(dto: TriviaNodeDto, missionId: string): MissionNode {
  return {
    id: dto.id,
    missionId,
    parentNodeId: dto.parentNodeId,
    type: "Trivia",
    title: "Trivia",
    description: "Trivia challenge",
    executionOrder: dto.executionOrder,
    baseScore: dto.baseScore,
    questions: toUiTriviaQuestions(dto.questions),
    hints: [],
  };
}

export function toTreasureHuntGameViewModel(dto: TreasureHuntNodeDto, missionId: string): MissionNode {
  return {
    id: dto.id,
    missionId,
    parentNodeId: dto.parentNodeId,
    type: "TreasureHunt",
    title: "Treasure Hunt",
    description: "Treasure hunt challenge",
    executionOrder: dto.executionOrder,
    baseScore: dto.baseScore,
    instructions: dto.instructions,
    secretCode: dto.secretCode,
    destination: {
      latitude: dto.destination.latitude,
      longitude: dto.destination.longitude,
    },
    hints: [],
  };
}

export async function loadStageGames(missionId: string, stageId: string, signal?: AbortSignal): Promise<MissionNode[]> {
  const summaries = await gameService.getGamesByStage(missionId, stageId, signal);
  if (signal?.aborted) return [];

  const children = await Promise.all(
    summaries.map(async (summary) => {
      if (summary.nodeType === "Trivia") {
        const detail = await gameService.getTrivia(missionId, summary.id, signal);
        return toTriviaGameViewModel(detail, missionId);
      }
      const detail = await gameService.getTreasureHunt(missionId, summary.id, signal);
      return toTreasureHuntGameViewModel(detail, missionId);
    }),
  );

  return children.sort((a, b) => a.executionOrder - b.executionOrder);
}

export const gameService = {
  async getGamesByStage(
    missionId: string,
    stageId: string,
    signal?: AbortSignal,
  ): Promise<StageGameDto[]> {
    return apiRequest<StageGameDto[]>(`/missions/${missionId}/nodes/${stageId}/games`, { signal });
  },

  async getTrivia(missionId: string, nodeId: string, signal?: AbortSignal): Promise<TriviaNodeDto> {
    return apiRequest<TriviaNodeDto>(`/missions/${missionId}/nodes/${nodeId}/trivia`, { signal });
  },

  async addTrivia(
    missionId: string,
    parentNodeId: string,
    command: AddTriviaNodeCommand,
  ): Promise<AddTriviaNodeResponse> {
    return apiRequest<AddTriviaNodeResponse>(`/missions/${missionId}/nodes/${parentNodeId}/trivia`, {
      method: "POST",
      body: command,
    });
  },

  async updateTrivia(
    missionId: string,
    nodeId: string,
    command: UpdateTriviaNodeCommand,
  ): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}/trivia`, {
      method: "PUT",
      body: command,
    });
  },

  async getTreasureHunt(
    missionId: string,
    nodeId: string,
    signal?: AbortSignal,
  ): Promise<TreasureHuntNodeDto> {
    return apiRequest<TreasureHuntNodeDto>(
      `/missions/${missionId}/nodes/${nodeId}/treasure-hunts`,
      { signal },
    );
  },

  async addTreasureHunt(
    missionId: string,
    parentNodeId: string,
    command: AddTreasureHuntNodeCommand,
  ): Promise<AddTreasureHuntNodeResponse> {
    return apiRequest<AddTreasureHuntNodeResponse>(
      `/missions/${missionId}/nodes/${parentNodeId}/treasure-hunts`,
      {
        method: "POST",
        body: command,
      },
    );
  },

  async updateTreasureHunt(
    missionId: string,
    nodeId: string,
    command: UpdateTreasureHuntNodeCommand,
  ): Promise<void> {
    await apiRequest<void>(`/missions/${missionId}/nodes/${nodeId}/treasure-hunts`, {
      method: "PUT",
      body: command,
    });
  },
};

export function logGameApiProblem(error: unknown): void {
  if (error instanceof ApiError && (error.status === 400 || error.status === 409)) {
    console.error("Game API ProblemDetails:", error.details);
  }
}

export function getGameApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Error inesperado al comunicarse con la API de juegos.";
  }

  if (error.status === 401) {
    return "Sesión expirada o no autorizada. Vuelve a iniciar sesión en Login (puerto 3002).";
  }

  if (isRecord(error.details)) {
    const errors = error.details.errors;
    if (isRecord(errors)) {
      const messages = Object.entries(errors).flatMap(([field, value]) => {
        if (Array.isArray(value)) {
          return value
            .filter((entry): entry is string => typeof entry === "string" && entry.length > 0)
            .map((entry) => (field === "$" ? entry : `${field}: ${entry}`));
        }
        return [];
      });
      if (messages.length > 0) {
        return messages.join(" ");
      }
    }

    const detail = error.details.detail;
    if (typeof detail === "string" && detail.length > 0) {
      return detail;
    }
    const title = error.details.title;
    if (
      typeof title === "string" &&
      title.length > 0 &&
      title !== "One or more validation errors occurred."
    ) {
      return title;
    }
  }

  if (error.status === 409) {
    return "Operación bloqueada por RN-01: la misión no está en borrador.";
  }

  if (error.status === 400) {
    return "Los datos del juego no pasaron la validación. Revisa el formulario.";
  }

  if (error.status === 404) {
    return "El juego o la etapa no fue encontrado.";
  }

  return error.message;
}
