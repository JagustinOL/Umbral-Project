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

  useEffect(() => {
    const controller = new AbortController();
    void loadTeams(controller.signal);

    const interval = setInterval(() => {
      void loadTeams(controller.signal);
    }, 5000);

    return () => {
      controller.abort();
      clearInterval(interval);
    };
  }, [loadTeams]);

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
