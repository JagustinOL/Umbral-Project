// Mock data for UMBRAL Operator Dashboard (MVP)

export interface Mission {
  id: string;
  title: string;
  description: string;
  difficulty: number;
  maxDurationMinutes?: number;
  stageCount: number;
}

export interface Team {
  id: string;
  name: string;
  memberCount: number;
  joinedAt: string;
  status: 'pending' | 'approved' | 'rejected';
}

export function getMockMissions(): Mission[] {
  return [
    {
      id: 'mission-001',
      title: 'Downtown Detective',
      description: 'Solve the mystery hidden in the city center. Combine clues and find the culprit.',
      difficulty: 7,
      maxDurationMinutes: 45,
      stageCount: 4,
    },
    {
      id: 'mission-002',
      title: 'Lost Treasure of the Harbor',
      description: 'Navigate through riddles to uncover the hidden treasure by the waterfront.',
      difficulty: 5,
      maxDurationMinutes: 30,
      stageCount: 3,
    },
    {
      id: 'mission-003',
      title: 'Museum Heist',
      description: 'Extract the artifact from the museum without triggering alarms. Quick thinking required.',
      difficulty: 9,
      maxDurationMinutes: 60,
      stageCount: 5,
    },
    {
      id: 'mission-004',
      title: 'Campus Code Breaker',
      description: 'Use cryptography and campus landmarks to break the ancient code.',
      difficulty: 6,
      maxDurationMinutes: 50,
      stageCount: 4,
    },
  ];
}

export function getMockTeams(): Team[] {
  return [
    {
      id: 'team-001',
      name: 'The Investigators',
      memberCount: 4,
      joinedAt: new Date(Date.now() - 5 * 60000).toLocaleTimeString(),
      status: 'approved',
    },
    {
      id: 'team-002',
      name: 'Code Crackers',
      memberCount: 3,
      joinedAt: new Date(Date.now() - 2 * 60000).toLocaleTimeString(),
      status: 'approved',
    },
  ];
}

// Types for backend integration
export interface OperatorRef {
  operatorId: string;
  firstName: string;
  lastName: string;
  email: string;
}

export interface LiveSession {
  sessionId: string;
  missionId: string;
  operatorId: string;
  joinCode: string;
  status: 'pending' | 'active' | 'paused' | 'finished';
  createdAt: Date;
  startedAt?: Date;
  finishedAt?: Date;
}

export interface TeamProgress {
  teamId: string;
  sessionId: string;
  teamName: string;
  currentNodeId: string;
  score: number;
  elapsedTime: number;
  status: 'active' | 'completed' | 'expelled';
}

export interface HintRelease {
  hintId: string;
  teamId: string;
  nodeId: string;
  releasedAt: Date;
  isManual: boolean;
}

export interface Penalty {
  penaltyId: string;
  teamId: string;
  points: number;
  reason: string;
  appliedAt: Date;
}
