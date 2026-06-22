import { CreateOperatorPayload, Operator } from "@/lib/types";
import { ApiError, userApiRequest } from "@/lib/api/client";
import {
  CreateOperatorRequest,
  CreateOperatorResponse,
  OperatorDto,
} from "@/lib/types/api";

export function toOperatorViewModel(dto: OperatorDto): Operator {
  return {
    id: dto.operatorId,
    firstName: dto.firstName,
    lastName: dto.lastName,
    email: dto.email,
    status: dto.isActive ? "Active" : "Inactive",
    assignedMissions: [],
  };
}

export const operatorService = {
  async getOperators(signal?: AbortSignal): Promise<OperatorDto[]> {
    return userApiRequest<OperatorDto[]>("/operators", { signal });
  },

  async createOperator(payload: CreateOperatorPayload): Promise<CreateOperatorResponse> {
    const request: CreateOperatorRequest = {
      firstName: payload.firstName,
      lastName: payload.lastName,
      email: payload.email,
    };

    return userApiRequest<CreateOperatorResponse>("/operators", {
      method: "POST",
      body: request,
    });
  },

  async deactivateOperator(operatorId: string): Promise<void> {
    await userApiRequest<void>(`/operators/${operatorId}/deactivate`, {
      method: "PUT",
    });
  },
};

export function getOperatorApiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Unexpected error while communicating with Operator API.";
  }

  if (error.status === 409) {
    return "Operation blocked by business rules. The email may already exist or the operator may have active sessions.";
  }

  if (error.status === 400) {
    return error.message || "Operator request failed validation. Refresh the list and try again.";
  }

  if (error.status === 404) {
    return "Operator was not found. Refresh the list and try again.";
  }

  return error.message;
}
