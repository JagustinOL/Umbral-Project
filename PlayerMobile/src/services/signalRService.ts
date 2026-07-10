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

export type LiveSessionHubCallbacks = {
  onSessionStateChanged?: (payload: SessionStatePayload) => void;
  onScoreUpdate?: (payload: ScoreUpdatePayload) => void;
  onManualPenalty?: (payload: ManualPenaltyPayload) => void;
  onReconnecting?: () => void;
  onReconnected?: () => void;
};

let connection: HubConnection | null = null;
let activeSessionId: string | null = null;

export async function connectLiveSessionHub(
  sessionId: string,
  callbacks: LiveSessionHubCallbacks,
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

  connection.onreconnecting(() => {
    callbacks.onReconnecting?.();
  });

  connection.onreconnected(() => {
    callbacks.onReconnected?.();
  });

  await connection.start();
  await connection.invoke('JoinSession', sessionId);
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
