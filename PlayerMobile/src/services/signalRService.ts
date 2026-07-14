import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { API_CONFIG, API_PATHS } from '../constants/api';
import { getAccessToken } from '../storage/secureTokenStorage';
import type { LiveSessionStatus, RankingEntry } from '../types/gameplay';

export type SessionStatePayload = {
  sessionId: string;
  previousStatus: string;
  newStatus: string;
  reason?: string | null;
};

export type ScoreUpdatePayload = {
  sessionId: string;
  teamId: string;
  newTotalScore: number;
  ranking: RankingEntry[];
};

export type ManualPenaltyPayload = {
  sessionId: string;
  teamId: string;
  penaltyPoints: number;
  reason: string;
};

export type HintReleasedPayload = {
  sessionId: string;
  teamId: string;
  hintId: string;
  missionNodeId: string;
  penaltyPoints: number;
};

export type SupportMessagePayload = {
  sessionId: string;
  teamId: string;
  message: string;
};

export type JoinRequestResolvedPayload = {
  sessionId: string;
  teamId: string;
  requestId: string;
  decision: string;
};

export type LiveSessionHubCallbacks = {
  onSessionStateChanged?: (payload: SessionStatePayload) => void;
  onScoreUpdate?: (payload: ScoreUpdatePayload) => void;
  onManualPenalty?: (payload: ManualPenaltyPayload) => void;
  onHintReleased?: (payload: HintReleasedPayload) => void;
  onSupportMessage?: (payload: SupportMessagePayload) => void;
  onJoinRequestResolved?: (payload: JoinRequestResolvedPayload) => void;
  onReconnecting?: () => void;
  onReconnected?: () => void;
};

let connection: HubConnection | null = null;
let activeSessionId: string | null = null;

export async function connectLiveSessionHub(
  sessionId: string,
  callbacks: LiveSessionHubCallbacks,
  teamId?: string,
): Promise<void> {
  if (
    connection &&
    activeSessionId === sessionId &&
    connection.state === HubConnectionState.Connected
  ) {
    return;
  }

  await disconnectLiveSessionHub();

  const token = await getAccessToken();
  if (!token) {
    throw new Error('Authentication required for live session.');
  }

  const hubUrl = `${API_CONFIG.sessionManagementBaseUrl}${API_PATHS.signalRHub}`;

  connection = new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on('ReceiveSessionStateChanged', (payload: SessionStatePayload) => {
    callbacks.onSessionStateChanged?.(payload);
  });

  connection.on('ReceiveScoreUpdate', (payload: ScoreUpdatePayload) => {
    callbacks.onScoreUpdate?.(payload);
  });

  connection.on('ReceiveManualPenalty', (payload: ManualPenaltyPayload) => {
    callbacks.onManualPenalty?.(payload);
  });

  connection.on('ReceiveHintReleased', (payload: HintReleasedPayload) => {
    callbacks.onHintReleased?.(payload);
  });

  connection.on('ReceiveSupportMessage', (payload: SupportMessagePayload) => {
    callbacks.onSupportMessage?.(payload);
  });

  connection.on('JoinRequestResolved', (payload: JoinRequestResolvedPayload) => {
    callbacks.onJoinRequestResolved?.(payload);
  });

  connection.onreconnecting(() => {
    callbacks.onReconnecting?.();
  });

  connection.onreconnected(() => {
    callbacks.onReconnected?.();
  });

  await connection.start();
  await connection.invoke('JoinSession', sessionId);
  if (teamId) {
    await connection.invoke('JoinTeamSession', sessionId, teamId);
  }
  activeSessionId = sessionId;
}

export async function disconnectLiveSessionHub(): Promise<void> {
  if (!connection) {
    return;
  }

  try {
    if (activeSessionId && connection.state === HubConnectionState.Connected) {
      await connection.invoke('LeaveSession', activeSessionId);
    }
    await connection.stop();
  } catch {
    // ignore teardown errors
  } finally {
    connection = null;
    activeSessionId = null;
  }
}

export function getHubConnectionState(): HubConnectionState | 'Disconnected' {
  return connection?.state ?? 'Disconnected';
}

export function mapHubStatus(status: string): LiveSessionStatus {
  return status;
}
