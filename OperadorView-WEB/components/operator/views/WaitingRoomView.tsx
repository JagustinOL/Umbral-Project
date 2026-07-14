'use client';

import { useCallback, useEffect, useState } from 'react';
import { ArrowLeftIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { TeamsList } from '../lists/TeamsList';
import { JoinCodeDisplay } from '../ui/JoinCodeDisplay';
import { StartSessionButton } from '../buttons/StartSessionButton';
import {
  getOperatorSessionApiErrorMessage,
  operatorSessionService,
} from '@/lib/services/operatorSessionService';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertTriangleIcon } from 'lucide-react';
import { toast } from 'sonner';
import { SessionJoinRequestDto } from '@/lib/types/api';

interface WaitingRoomViewProps {
  operatorId: string;
  sessionId: string;
  missionTitle: string;
  joinCode: string;
  onBack: () => void;
  onStartSession: () => Promise<void>;
}

export function WaitingRoomView({
  operatorId,
  sessionId,
  missionTitle,
  joinCode,
  onBack,
  onStartSession,
}: WaitingRoomViewProps) {
  const [copiedCode, setCopiedCode] = useState(false);
  const [teamIds, setTeamIds] = useState<string[]>([]);
  const [isLoadingTeams, setIsLoadingTeams] = useState(true);
  const [teamsError, setTeamsError] = useState<string | null>(null);
  const [sessionStarting, setSessionStarting] = useState(false);
  const [joinRequests, setJoinRequests] = useState<SessionJoinRequestDto[]>([]);
  const [isLoadingRequests, setIsLoadingRequests] = useState(true);
  const [requestActionTeamId, setRequestActionTeamId] = useState<string | null>(null);

  const loadTeams = useCallback(
    async (signal?: AbortSignal) => {
      setTeamsError(null);
      try {
        const result = await operatorSessionService.getSessionTeams(
          operatorId,
          sessionId,
          signal,
        );
        setTeamIds(result.teamIds);
      } catch (error) {
        if (signal?.aborted) return;
        setTeamsError(getOperatorSessionApiErrorMessage(error));
      } finally {
        if (!signal?.aborted) {
          setIsLoadingTeams(false);
        }
      }
    },
    [operatorId, sessionId],
  );

  const loadJoinRequests = useCallback(
    async (signal?: AbortSignal) => {
      try {
        setJoinRequests(await operatorSessionService.getJoinRequests(sessionId, signal));
      } catch (error) {
        if (!signal?.aborted) toast.error(getOperatorSessionApiErrorMessage(error));
      } finally {
        if (!signal?.aborted) setIsLoadingRequests(false);
      }
    },
    [sessionId],
  );

  useEffect(() => {
    const controller = new AbortController();
    void loadTeams(controller.signal);
    void loadJoinRequests(controller.signal);

    const interval = setInterval(() => {
      void loadTeams(controller.signal);
      void loadJoinRequests(controller.signal);
    }, 5000);

    return () => {
      controller.abort();
      clearInterval(interval);
    };
  }, [loadJoinRequests, loadTeams]);

  const handleDecision = async (teamId: string, decision: 'Approve' | 'Reject') => {
    setRequestActionTeamId(teamId);
    try {
      await operatorSessionService.decideJoinRequest(sessionId, teamId, decision);
      toast.success(decision === 'Approve' ? 'Equipo aprobado.' : 'Solicitud rechazada.');
      await Promise.all([loadJoinRequests(), loadTeams()]);
    } catch (error) {
      toast.error(getOperatorSessionApiErrorMessage(error));
    } finally {
      setRequestActionTeamId(null);
    }
  };

  const handleCopyCode = () => {
    void navigator.clipboard.writeText(joinCode);
    setCopiedCode(true);
    setTimeout(() => setCopiedCode(false), 2000);
  };

  const handleStartSession = async () => {
    setSessionStarting(true);
    try {
      await onStartSession();
    } catch (error) {
      toast.error(getOperatorSessionApiErrorMessage(error));
    } finally {
      setSessionStarting(false);
    }
  };

  const approvedTeamCount = teamIds.length;

  return (
    <div>
      <div className="mb-8 flex items-center gap-4">
        <Button onClick={onBack} variant="ghost" size="icon" className="shrink-0">
          <ArrowLeftIcon className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">{missionTitle}</h1>
          <p className="text-sm text-muted-foreground mt-1">Sala de espera · sesión pendiente</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <JoinCodeDisplay code={joinCode} onCopy={handleCopyCode} copied={copiedCode} />

          <div>
            <h2 className="text-sm font-medium text-foreground mb-3">
              Equipos registrados ({approvedTeamCount})
            </h2>

            {teamsError && (
              <Alert variant="destructive" className="mb-4">
                <AlertTriangleIcon className="h-4 w-4" />
                <AlertTitle>Error al cargar equipos</AlertTitle>
                <AlertDescription>{teamsError}</AlertDescription>
              </Alert>
            )}

            <TeamsList teamIds={teamIds} isLoading={isLoadingTeams} />
          </div>

          <div>
            <h2 className="text-sm font-medium text-foreground mb-3">
              Solicitudes pendientes
            </h2>
            {isLoadingRequests ? (
              <p className="text-sm text-muted-foreground">Cargando solicitudes…</p>
            ) : (
              <div className="space-y-3">
                {joinRequests.filter((request) => request.status.toLowerCase() === 'pending').length === 0 ? (
                  <p className="text-sm text-muted-foreground">No hay solicitudes pendientes.</p>
                ) : (
                  joinRequests
                    .filter((request) => request.status.toLowerCase() === 'pending')
                    .map((request) => (
                      <div
                        key={request.requestId}
                        className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-card p-4"
                      >
                        <div>
                          <p className="font-medium text-foreground">{request.teamName ?? request.teamId}</p>
                          <p className="text-xs text-muted-foreground">
                            Solicitada {new Date(request.requestedAtUtc).toLocaleString()}
                          </p>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            size="sm"
                            disabled={requestActionTeamId === request.teamId}
                            onClick={() => void handleDecision(request.teamId, 'Approve')}
                          >
                            Aprobar
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            disabled={requestActionTeamId === request.teamId}
                            onClick={() => void handleDecision(request.teamId, 'Reject')}
                          >
                            Rechazar
                          </Button>
                        </div>
                      </div>
                    ))
                )}
              </div>
            )}
          </div>
        </div>

        <div className="lg:col-span-1">
          <div className="sticky top-8 rounded-lg border border-border bg-card p-5 space-y-5">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Estado
              </p>
              <p className="text-sm font-medium text-amber-700 mt-1">Pendiente</p>
            </div>

            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Equipos listos
              </p>
              <p className="text-3xl font-semibold text-foreground mt-1">{approvedTeamCount}</p>
            </div>

            <StartSessionButton
              disabled={approvedTeamCount === 0}
              loading={sessionStarting}
              onStart={() => void handleStartSession()}
            />

            {approvedTeamCount === 0 && (
              <p className="text-xs text-destructive bg-destructive/5 border border-destructive/20 rounded-md px-3 py-2">
                Se requiere al menos un equipo registrado para iniciar la sesión.
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
