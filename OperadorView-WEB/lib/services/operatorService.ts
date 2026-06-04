import { ApiError, missionApiRequest } from "@/lib/api/client";
import { OperatorDto } from "@/lib/types/api";

export const operatorService = {
  async getOperators(signal?: AbortSignal): Promise<OperatorDto[]> {
    return missionApiRequest<OperatorDto[]>("/operators", { signal });
  },

  async getOperatorById(operatorId: string, signal?: AbortSignal): Promise<OperatorDto | null> {
    const operators = await this.getOperators(signal);
    return operators.find((o) => o.operatorId === operatorId) ?? null;
  },
};

export function getOperatorProfileErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "No se pudo cargar el perfil del operador.";
  }

  return error.message;
}
