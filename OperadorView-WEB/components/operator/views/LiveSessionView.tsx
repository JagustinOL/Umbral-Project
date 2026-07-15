'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { ArrowLeftIcon, PauseIcon, PlayIcon } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { ApiError } from '@/lib/api/client';
import { operatorSessionService, getOperatorSessionApiErrorMessage } from '@/lib/services/operatorSessionService';
import {
  OperatorSessionBoardDto,
  OperatorTeamBoardEntryDto,
  RankingEntryDto,
} from '@/lib/types/api';

interface LiveSessionViewProps {
  operatorId: string;
  sessionId: string;
  missionTitle: string;
  onBack: () => void;
  onFinalized: () => void;
}

function isSessionAlreadyClosedError(error: unknown): boolean {
  if (!(error instanceof ApiError) || error.status !== 409) return false;
  return /sesión ya está cerrada|Finalized|Cancelled|inválida/i.test(error.message);
}

export function LiveSessionView({
  operatorId,
  sessionId,
  missionTitle,
  onBack,
  onFinalized,
}: LiveSessionViewProps) {
  const [board, setBoard] = useState<OperatorSessionBoardDto | null>(null);
  const [ranking, setRanking] = useState<RankingEntryDto[]>([]);
  const [isPaused, setIsPaused] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isActing, setIsActing] = useState(false);
  const [releasingHintId, setReleasingHintId] = useState<string | null>(null);
  const closedHandledRef = useRef(false);

  const handleSessionClosed = useCallback(() => {
    if (closedHandledRef.current) return;
    closedHandledRef.current = true;
    toast.success('La sesión ya está finalizada.');
    onFinalized();
  }, [onFinalized]);

  const loadSession = useCallback(async (signal?: AbortSignal) => {
    try {
      const [nextBoard, nextRanking] = await Promise.all([
        operatorSessionService.getOperatorBoard(sessionId, signal),
        operatorSessionService.getRanking(sessionId, signal),
      ]);
      if (signal?.aborted) return;

      const status = nextBoard.sessionStatus.toLowerCase();
      if (status === 'finalized' || status === 'cancelled') {
        setBoard(nextBoard);
        setRanking(nextRanking);
        handleSessionClosed();
        return;
      }

      setBoard(nextBoard);
      setRanking(nextRanking);
      setIsPaused(status === 'paused');
    } catch (error) {
      if (signal?.aborted) return;
      if (isSessionAlreadyClosedError(error)) {
        handleSessionClosed();
        return;
      }
      toast.error(getOperatorSessionApiErrorMessage(error));
    } finally {
      if (!signal?.aborted) setIsLoading(false);
    }
  }, [handleSessionClosed, sessionId]);

  useEffect(() => {
    closedHandledRef.current = false;
    const controller = new AbortController();
    void loadSession(controller.signal);
    const interval = setInterval(() => {
      if (!closedHandledRef.current) void loadSession(controller.signal);
    }, 5000);
    return () => {
      controller.abort();
      clearInterval(interval);
    };
  }, [loadSession]);

  const runAction = async (action: () => Promise<void>, success: string): Promise<boolean> => {
    setIsActing(true);
    try {
      await action();
    } catch (error) {
      toast.error(getOperatorSessionApiErrorMessage(error));
      return false;
    } finally {
      setIsActing(false);
    }
    toast.success(success);
    await loadSession();
    return true;
  };

  const releaseHint = async (teamId: string, hintId: string) => {
    setReleasingHintId(hintId);
    try {
      await runAction(
        () => operatorSessionService.releaseHint(sessionId, teamId, hintId),
        'Pista liberada.',
      );
    } finally {
      setReleasingHintId(null);
    }
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

  const teams: OperatorTeamBoardEntryDto[] = board?.teams ?? [];
  const allTeamsCompleted =
    teams.length > 0 && teams.every((team) => team.isMissionCompleted);

  return (
    <div>
      <div className="mb-8 flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button onClick={onBack} variant="ghost" size="icon"><ArrowLeftIcon className="h-5 w-5" /></Button>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-foreground">{missionTitle}</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Sesión en vivo
              {board ? ` · ${board.sessionStatus}` : ''}
            </p>
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
            if (!window.confirm('¿Finalizar la sesión? Esta acción no se puede revertir.')) return;
            setIsActing(true);
            void (async () => {
              try {
                await operatorSessionService.finalizeSession(operatorId, sessionId);
                toast.success('Sesión finalizada.');
                closedHandledRef.current = true;
                onFinalized();
              } catch (error) {
                if (isSessionAlreadyClosedError(error)) {
                  handleSessionClosed();
                  return;
                }
                toast.error(getOperatorSessionApiErrorMessage(error));
              } finally {
                setIsActing(false);
              }
            })();
          }}>
            Finalizar sesión
          </Button>
        </div>
      </div>

      {allTeamsCompleted && (
        <p className="mb-6 text-sm text-amber-800 bg-amber-50 border border-amber-200 rounded-md px-3 py-2">
          Todos los equipos completaron la misión. Use «Finalizar sesión» para cerrarla y liberar la misión.
        </p>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <section className="space-y-4 lg:col-span-2">
          <h2 className="text-sm font-medium text-foreground">Equipos aprobados ({teams.length})</h2>
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Cargando equipos…</p>
          ) : teams.length === 0 ? (
            <p className="text-sm text-muted-foreground">Aún no hay equipos en la sesión.</p>
          ) : (
            teams.map((team) => {
              const displayName =
                team.teamName
                ?? ranking.find((item) => item.teamId === team.teamId)?.teamName
                ?? team.teamId;
              const progressLabel = team.isMissionCompleted
                ? 'Misión completada'
                : team.currentGameLabel
                  ?? 'Sin juego actual';

              return (
                <div key={team.teamId} className="rounded-lg border border-border bg-card p-4 space-y-4">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="font-medium text-foreground">{displayName}</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Progreso · {progressLabel}
                        {team.participationStatus.toLowerCase() !== 'active'
                          ? ` · ${team.participationStatus}`
                          : ''}
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <Button size="sm" variant="outline" disabled={isActing} onClick={() => applyPenalty(team.teamId)}>
                        Aplicar penalización
                      </Button>
                      <Button size="sm" variant="outline" disabled={isActing} onClick={() => sendMessage(team.teamId)}>
                        Enviar mensaje
                      </Button>
                    </div>
                  </div>

                  <div>
                    <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      Pistas disponibles
                    </h3>
                    {team.isMissionCompleted || !team.currentNodeId ? (
                      <p className="text-sm text-muted-foreground">
                        No hay juego activo; no se pueden liberar pistas.
                      </p>
                    ) : team.availableHints.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        No hay pistas pendientes en el juego actual
                        {team.releasedHints.some((h) => h.missionNodeId === team.currentNodeId)
                          ? ' (todas liberadas).'
                          : '.'}
                      </p>
                    ) : (
                      <ul className="space-y-2">
                        {team.availableHints.map((hint) => (
                          <li
                            key={hint.hintId}
                            className="flex flex-wrap items-start justify-between gap-3 rounded-md border border-border/80 bg-background/40 px-3 py-2"
                          >
                            <div className="min-w-0 flex-1">
                              {hint.nodePrompt ? (
                                <p className="text-xs font-semibold text-foreground/80">
                                  {hint.nodeType === 'Trivia' || hint.nodeType === 'trivia'
                                    ? `Pregunta · ${hint.nodePrompt}`
                                    : hint.nodePrompt}
                                </p>
                              ) : null}
                              <p className="mt-0.5 text-xs font-medium text-muted-foreground">
                                Pista · orden {hint.order}
                                {hint.penaltyPoints > 0 ? ` · −${hint.penaltyPoints} pts` : ''}
                              </p>
                              <p className="mt-0.5 text-sm text-foreground">{hint.content}</p>
                            </div>
                            <Button
                              size="sm"
                              disabled={isActing || releasingHintId === hint.hintId || isPaused}
                              onClick={() => void releaseHint(team.teamId, hint.hintId)}
                            >
                              Liberar
                            </Button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  {team.releasedHints.length > 0 ? (
                    <div>
                      <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                        Pistas liberadas ({team.releasedHints.length})
                      </h3>
                      <ul className="space-y-1 text-sm text-muted-foreground">
                        {team.releasedHints.map((hint) => (
                          <li key={`${hint.hintId}-${hint.releasedAtUtc}`}>
                            {hint.nodePrompt
                              ? `${hint.nodeType === 'Trivia' || hint.nodeType === 'trivia' ? 'Pregunta' : 'Juego'} · ${hint.nodePrompt} · `
                              : ''}
                            {new Date(hint.releasedAtUtc).toLocaleTimeString()}
                            {' · '}
                            −{hint.penaltyPoints} pts
                            {hint.wasManualRelease ? ' · manual' : ' · auto'}
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}
                </div>
              );
            })
          )}
        </section>
        <aside className="rounded-lg border border-border bg-card p-5">
          <h2 className="mb-4 text-sm font-medium text-foreground">Ranking</h2>
          <ol className="space-y-3">
            {ranking.map((entry, index) => (
              <li key={entry.teamId} className="flex items-center justify-between text-sm">
                <span className="text-foreground">
                  {(entry.position ?? index + 1)}. {entry.teamName}
                </span>
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
