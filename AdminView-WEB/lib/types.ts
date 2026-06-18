// ─── Enums ────────────────────────────────────────────────────────────────────

export type MissionStatus = "Draft" | "Active" | "Inactive";
export type MissionNodeType = "Stage" | "Trivia" | "TreasureHunt";
export type OperatorStatus = "Active" | "Inactive";

// ─── Value Objects ────────────────────────────────────────────────────────────

export interface GpsCoordinate {
  latitude: number;
  longitude: number;
}

export interface TriviaQuestion {
  id: string;
  questionText: string;
  options: TriviaOption[];
}

export interface TriviaOption {
  id: string;
  text: string;
  isCorrect: boolean;
}

// ─── Domain Entities ──────────────────────────────────────────────────────────

export interface Hint {
  id: string;
  nodeId: string;
  content: string;
  order: number;
  penaltyPoints: number;
}

export interface MissionNode {
  id: string;
  missionId: string;
  parentNodeId?: string;
  type: MissionNodeType;
  title: string;
  description: string;
  executionOrder: number;
  baseScore?: number;
  children?: MissionNode[];
  hints?: Hint[];
  // Trivia-specific
  questions?: TriviaQuestion[];
  // TreasureHunt-specific
  instructions?: string;
  secretCode?: string;
  destination?: GpsCoordinate;
}

export interface Mission {
  id: string;
  title: string;
  description: string;
  difficulty: number;
  maxDurationMinutes?: number;
  status: MissionStatus;
  nodes?: MissionNode[];
  assignedOperators?: OperatorRef[];
}

export interface OperatorRef {
  operatorId: string;
}

export interface Operator {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  status: OperatorStatus;
  assignedMissions?: string[]; // mission IDs
}

// ─── Request Payloads ─────────────────────────────────────────────────────────

export interface CreateMissionPayload {
  title: string;
  description: string;
  difficulty: number;
  maxDurationMinutes?: number;
}

export interface UpdateMissionPayload {
  title: string;
  description: string;
  maxDurationMinutes?: number;
}

export interface AddRootNodePayload {
  title: string;
  description: string;
  executionOrder: number;
}

export interface UpdateNodePayload {
  title: string;
  description: string;
}

export interface AddTriviaNodePayload {
  questions: TriviaQuestion[];
  executionOrder: number;
}

export interface AddTreasureHuntPayload {
  instructions: string;
  secretCode: string;
  destination: GpsCoordinate;
  executionOrder: number;
}

export interface CreateOperatorPayload {
  firstName: string;
  lastName: string;
  email: string;
}

export interface AssignOperatorPayload {
  operatorId: string;
}
