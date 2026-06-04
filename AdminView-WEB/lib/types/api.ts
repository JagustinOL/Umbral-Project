export type MissionStatus = "Draft" | "Active" | "Inactive";
export type MissionDifficulty = "Easy" | "Medium" | "Hard";
export type MissionDifficultyLevel = 1 | 2 | 3;
export type MissionNodeType = "Stage" | "Trivia" | "TreasureHunt";

export interface MissionDto {
  id: string;
  title: string;
  description: string;
  status: MissionStatus;
  difficulty: MissionDifficulty;
  difficultyScoreMultiplier: number;
  maxDurationMinutes?: number;
  createdAtUtc: string;
  lastModifiedAtUtc?: string | null;
  operatorIds: string[];
}

export interface CreateMissionRequest {
  title: string;
  description: string;
  difficulty: MissionDifficultyLevel;
  maxDurationMinutes?: number;
}

export interface CreateMissionResponse {
  id: string;
}

export interface UpdateMissionDetailsRequest {
  title: string;
  description: string;
  maxDurationMinutes?: number;
}

export interface DeactivateMissionCommand {}

export interface MissionHasOpenSessionsResponse {
  hasOpenSessions: boolean;
}

export interface MissionNodeDto {
  id: string;
  title: string;
  description: string;
  nodeType: MissionNodeType;
  executionOrder: number;
  baseScore: number;
  parentNodeId?: string | null;
}

/** POST /api/v1/missions/{missionId}/nodes — AddRootNodeCommand (HU-05) */
export interface AddRootNodeCommand {
  title: string;
  description: string;
  executionOrder: number;
}

export interface AddRootNodeResponse {
  id: string;
}

/** PUT /api/v1/missions/{missionId}/nodes/{nodeId} — UpdateNodeCommand (HU-07) */
export interface UpdateNodeCommand {
  title: string;
  description: string;
}

/** @deprecated Use AddRootNodeCommand */
export type CreateStageRequest = AddRootNodeCommand;

/** @deprecated Use AddRootNodeResponse */
export type CreateStageResponse = AddRootNodeResponse;

/** @deprecated Use UpdateNodeCommand */
export type UpdateStageRequest = UpdateNodeCommand;

export interface StageGameDto {
  id: string;
  nodeType: Exclude<MissionNodeType, "Stage">;
  executionOrder: number;
  baseScore: number;
  title: string;
}

/** POST …/nodes/{parentNodeId}/trivia — AddTriviaNodeCommand (HU-09) */
export interface TriviaQuestionPayload {
  prompt: string;
  options: string[];
  correctOptionIndex: number;
}

export interface AddTriviaNodeCommand {
  questions: TriviaQuestionPayload[];
  executionOrder: number;
}

/** PUT …/nodes/{nodeId}/trivia — UpdateTriviaNodeCommand (HU-11) */
export interface UpdateTriviaNodeCommand {
  questions: TriviaQuestionPayload[];
}

export interface TriviaQuestionDto {
  prompt: string;
  options: string[];
  correctOptionIndex: number;
}

/** GET …/nodes/{nodeId}/trivia — HU-10 */
export interface TriviaNodeDto {
  id: string;
  missionId: string;
  parentNodeId: string;
  nodeType: string;
  executionOrder: number;
  baseScore: number;
  questions: TriviaQuestionDto[];
}

export interface AddTriviaNodeResponse {
  id: string;
}

export interface GpsCoordinateDto {
  latitude: number;
  longitude: number;
}

/** POST …/nodes/{parentNodeId}/treasure-hunts — AddTreasureHuntNodeCommand (HU-13) */
export interface AddTreasureHuntNodeCommand {
  instructions: string;
  secretCode: string;
  destination: GpsCoordinateDto;
  executionOrder: number;
}

/** PUT …/nodes/{nodeId}/treasure-hunts — UpdateTreasureHuntNodeCommand (HU-15) */
export interface UpdateTreasureHuntNodeCommand {
  instructions: string;
  secretCode: string;
  destination: GpsCoordinateDto;
}

/** GET …/nodes/{nodeId}/treasure-hunts — HU-14 */
export interface TreasureHuntNodeDto {
  id: string;
  missionId: string;
  parentNodeId: string;
  nodeType: string;
  executionOrder: number;
  baseScore: number;
  instructions: string;
  secretCode: string;
  destination: GpsCoordinateDto;
}

export interface AddTreasureHuntNodeResponse {
  id: string;
}

/** GET /api/v1/missions/{missionId}/nodes/{nodeId}/hints — HU-18 */
export interface HintDto {
  id: string;
  order: number;
  content: string;
  penaltyPoints: number;
}

/** POST …/hints (multipart/form-data) — AddHintCommand (HU-17) */
export interface AddHintResponse {
  id: string;
}

/** PUT …/hints/{hintId} — UpdateHintCommand (HU-19) */
export interface UpdateHintCommand {
  content: string;
}

export interface CreateOperatorRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface CreateOperatorResponse {
  id: string;
}

export interface OperatorDto {
  operatorId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

export interface DeactivateOperatorCommand {}

/** POST /api/v1/missions/{missionId}/operators — AssignOperatorToMissionCommand (HU-24) */
export interface AssignOperatorToMissionCommand {
  operatorId: string;
}

/** DELETE /api/v1/missions/{missionId}/operators/{operatorId} — RevokeOperatorFromMissionCommand (HU-25) */
export interface RevokeOperatorFromMissionCommand {}
