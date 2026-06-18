import { CreateOperatorPayload, Operator } from "@/lib/types";
import { ApiError, apiRequest } from "@/lib/api/client";
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
    return apiRequest<OperatorDto[]>("/operators", { signal });
  },

  async createOperator(payload: CreateOperatorPayload): Promise<CreateOperatorResponse> {
    const request: CreateOperatorRequest = {
      firstName: payload.firstName,
      lastName: payload.lastName,
      email: payload.email,
    };

    return apiRequest<CreateOperatorResponse>("/operators", {
      method: "POST",
      body: request,
    });
  },

  async deactivateOperator(operatorId: string): Promise<void> {
    await apiRequest<void>(`/operators/${operatorId}/deactivate`, {
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
    return "Operator payload failed validation. Verify the form fields and password policy.";
  }

  if (error.status === 404) {
    return "Operator was not found. Refresh the list and try again.";
  }

  return error.message;
}
