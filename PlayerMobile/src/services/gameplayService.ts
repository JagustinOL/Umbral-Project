import { API_CONFIG, API_PATHS } from '../constants/api';
import type {
  RankingEntry,
  SubmissionResult,
  SubmittedEvidence,
  TeamCurrentStage,
  TeamFinalSummary,
  TeamHint,
  TeamPenalty,
} from '../types/gameplay';
import { normalizeGuid } from '../utils/uuid';
import { apiFormRequest, apiRequest } from './apiClient';

const sessionBase = API_CONFIG.sessionManagementBaseUrl;
const scoringBase = API_CONFIG.scoringAuditBaseUrl;

export async function getTeamCurrentStage(
  sessionId: string,
  teamId: string,
): Promise<TeamCurrentStage> {
  return apiRequest<TeamCurrentStage>(
    API_PATHS.teamCurrentStage(
      normalizeGuid(sessionId),
      normalizeGuid(teamId),
    ),
    { baseUrl: sessionBase },
  );
}

export async function submitTriviaAnswer(input: {
  sessionId: string;
  teamId: string;
  nodeId: string;
  answer: string;
}): Promise<SubmissionResult> {
  return apiRequest<SubmissionResult>(
    API_PATHS.submitTrivia(input.sessionId, input.teamId),
    {
      method: 'POST',
      baseUrl: sessionBase,
      body: {
        nodeId: normalizeGuid(input.nodeId),
        answer: input.answer.trim(),
      },
    },
  );
}

export async function submitTreasureHuntCode(input: {
  sessionId: string;
  teamId: string;
  nodeId: string;
  foundCode: string;
}): Promise<SubmissionResult> {
  return apiRequest<SubmissionResult>(
    API_PATHS.submitTreasure(input.sessionId, input.teamId),
    {
      method: 'POST',
      baseUrl: sessionBase,
      body: {
        nodeId: normalizeGuid(input.nodeId),
        foundCode: input.foundCode.trim(),
      },
    },
  );
}

export async function submitEvidence(input: {
  sessionId: string;
  teamId: string;
  nodeId: string;
  content: string;
}): Promise<SubmittedEvidence> {
  const form = new FormData();
  form.append('nodeId', normalizeGuid(input.nodeId));
  form.append('content', input.content.trim());

  return apiFormRequest<SubmittedEvidence>(
    API_PATHS.submitEvidence(input.sessionId, input.teamId),
    { baseUrl: sessionBase, formData: form },
  );
}

export async function getTeamHints(
  sessionId: string,
  teamId: string,
): Promise<TeamHint[]> {
  return apiRequest<TeamHint[]>(
    API_PATHS.teamHints(sessionId, teamId),
    { baseUrl: sessionBase },
  );
}

export async function getTeamPenalties(
  sessionId: string,
  teamId: string,
): Promise<TeamPenalty[]> {
  return apiRequest<TeamPenalty[]>(
    API_PATHS.teamPenalties(sessionId, teamId),
    { baseUrl: scoringBase },
  );
}

export async function getSessionRanking(
  sessionId: string,
): Promise<RankingEntry[]> {
  return apiRequest<RankingEntry[]>(
    API_PATHS.sessionRanking(sessionId),
    { baseUrl: scoringBase },
  );
}

export async function getTeamFinalSummary(
  sessionId: string,
  teamId: string,
): Promise<TeamFinalSummary> {
  return apiRequest<TeamFinalSummary>(
    API_PATHS.teamSummary(sessionId, teamId),
    { baseUrl: sessionBase },
  );
}
