'use client';

import { useCallback, useEffect, useState } from 'react';
import { ArrowLeftIcon, PauseIcon, PlayIcon } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { operatorSessionService, getOperatorSessionApiErrorMessage } from '@/lib/services/operatorSessionService';
import { RankingEntryDto } from '@/lib/types/api';

interface LiveSessionViewProps {
  operatorId: string;
  sessionId: string;
  missionTitle: string;
  onBack: () => void;
  onFinalized: () => void;
}

export function LiveSessionView({
  operatorId,
  sessionId,
  missionTitle,
  onBack,
  onFinalized,
}: LiveSessionViewProps) {
  const [teamIds, setTeamIds] = useState<string[]>([]);
  const [ranking, setRanking] = useState<RankingEntryDto[]>([]);
  const [isPaused, setIsPaused] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isActing, setIsActing] = useState(false);

  const loadSession = useCallback(async (signal?: AbortSignal) => {
    try {
      const [teams, nextRanking] = await Promise.all([
        operatorSessionService.getSessionTeams(operatorId, sessionId, signal),
        operatorSessionService.getRanking(sessionId, signal),
      ]);
      setTeamIds(teams.teamIds);
      setRanking(nextRanking);
    } catch (error) {
      if (!signal?.aborted) toast.error(getOperatorSessionApiErrorMessage(error));
    } finally {
      if (!signal?.aborted) setIsLoading(false);
    }
  }, [operatorId, sessionId]);

  useEffect(() => {
    const controller = new AbortController();
    void loadSession(controller.signal);
    const interval = setInterval(() => void loadSession(controller.signal), 5000);
    return () => {
      controller.abort();
      clearInterval(interval);
    };
  }, [loadSession]);

  const runAction = async (action: () => Promise<void>, success: string): Promise<boolean> => {
    setIsActing(true);
    try {
      await action();
      toast.success(success);
      await loadSession();
      return true;
    } catch (error) {
      toast.error(getOperatorSessionApiErrorMessage(error));
      return false;
    } finally {
      setIsActing(false);
    }
  };

  const releaseHint = (teamId: string) => {
    const hintId = window.prompt('ID de la pista a liberar:')?.trim();
    if (hintId) void runAction(() => operatorSessionService.releaseHint(sessionId, teamId, hintId), 'Pista liberada.');
  };

  const applyPenalty = (teamId: string) => {
    const pointsRaw = window.prompt('Puntos a penalizar:')?.trim();
    if (!pointsRaw) return;
    const points = Number(pointsRaw);
    const reason = window.prompt('Motivo de la penalización:')?.trim();
    if (!Number.isInteger(points) || points <= 0 || !reason) {
      toast.error('Indica puntos positivos y un motivo para la penalización.');
      return;
    }
    void runAction(() => operatorSessionService.applyPenalty(sessionId, teamId, points, reason), 'Penalización aplicada.');
  };

  const sendMessage = (teamId: string) => {
    const message = window.prompt('Mensaje de soporte:')?.trim();
    if (message) void runAction(() => operatorSessionService.sendMessage(sessionId, teamId, message), 'Mensaje enviado.');
  };

  return (
    <div>
      <div className="mb-8 flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button onClick={onBack} variant="ghost" size="icon"><ArrowLeftIcon className="h-5 w-5" /></Button>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-foreground">{missionTitle}</h1>
            <p className="mt-1 text-sm text-muted-foreground">Sesión en vivo</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" disabled={isActing} onClick={() => void runAction(async () => {
            const result = await operatorSessionService.togglePause(sessionId);
            setIsPaused(result.status.toLowerCase() === 'paused');
          }, isPaused ? 'Sesión reanudada.' : 'Sesión pausada.')}>
            {isPaused ? <PlayIcon className="mr-2 h-4 w-4" /> : <PauseIcon className="mr-2 h-4 w-4" />}
            {isPaused ? 'Reanudar' : 'Pausar'}
          </Button>
          <Button variant="destructive" disabled={isActing} onClick={() => {
            if (window.confirm('¿Finalizar la sesión? Esta acción no se puede revertir.')) {
              void runAction(() => operatorSessionService.finalizeSession(operatorId, sessionId), 'Sesión finalizada.').then((succeeded) => {
                if (succeeded) onFinalized();
              });
            }
          }}>
            Finalizar sesión
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <section className="space-y-3 lg:col-span-2">
          <h2 className="text-sm font-medium text-foreground">Equipos aprobados ({teamIds.length})</h2>
          {isLoading ? <p className="text-sm text-muted-foreground">Cargando equipos…</p> : teamIds.map((teamId) => (
            <div key={teamId} className="rounded-lg border border-border bg-card p-4">
              <p className="mb-3 font-medium text-foreground">{ranking.find((item) => item.teamId === teamId)?.teamName ?? teamId}</p>
              <div className="flex flex-wrap gap-2">
                <Button size="sm" variant="outline" disabled={isActing} onClick={() => releaseHint(teamId)}>Liberar pista</Button>
                <Button size="sm" variant="outline" disabled={isActing} onClick={() => applyPenalty(teamId)}>Aplicar penalización</Button>
                <Button size="sm" variant="outline" disabled={isActing} onClick={() => sendMessage(teamId)}>Enviar mensaje</Button>
              </div>
            </div>
          ))}
        </section>
        <aside className="rounded-lg border border-border bg-card p-5">
          <h2 className="mb-4 text-sm font-medium text-foreground">Ranking</h2>
          <ol className="space-y-3">
            {ranking.map((entry, index) => (
              <li key={entry.teamId} className="flex items-center justify-between text-sm">
                <span className="text-foreground">{index + 1}. {entry.teamName}</span>
                <span className="font-medium text-foreground">{entry.totalScore} pts</span>
              </li>
            ))}
            {ranking.length === 0 && <li className="text-sm text-muted-foreground">Aún no hay puntajes.</li>}
          </ol>
        </aside>
      </div>
    </div>
  );
}
