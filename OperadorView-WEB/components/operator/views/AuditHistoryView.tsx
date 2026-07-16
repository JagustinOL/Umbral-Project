'use client';

import { useEffect, useState } from 'react';
import { ArrowLeftIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { getAuditEventDetail, getAuditEventTypeLabel } from '@/lib/auditDisplay';
import { getOperatorSessionApiErrorMessage, operatorSessionService } from '@/lib/services/operatorSessionService';
import { HistoricalSessionDto, SessionAuditDetailDto } from '@/lib/types/api';

export function AuditHistoryView() {
  const [sessions, setSessions] = useState<HistoricalSessionDto[]>([]);
  const [detail, setDetail] = useState<SessionAuditDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void operatorSessionService.getHistoricalSessions()
      .then((result) => setSessions(result.items))
      .catch((requestError) => setError(getOperatorSessionApiErrorMessage(requestError)));
  }, []);

  const openDetail = async (sessionId: string) => {
    try {
      setError(null);
      setDetail(await operatorSessionService.getSessionAuditDetail(sessionId));
    } catch (requestError) {
      setError(getOperatorSessionApiErrorMessage(requestError));
    }
  };

  if (detail) {
    return (
      <div>
        <div className="mb-8 flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => setDetail(null)}><ArrowLeftIcon className="h-5 w-5" /></Button>
          <div><h1 className="text-2xl font-semibold text-foreground">Auditoría de sesión</h1><p className="mt-1 text-sm text-muted-foreground">{detail.status}</p></div>
        </div>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <section className="lg:col-span-2">
            <h2 className="mb-3 text-sm font-medium text-foreground">Línea de tiempo</h2>
            <div className="space-y-3">
              {detail.timeline.map((event) => {
                const detailLine = getAuditEventDetail(event.metadata, event.missionNodeId);
                return (
                  <div key={event.eventId} className="rounded-lg border border-border bg-card p-4">
                    <p className="font-medium text-foreground">{event.description}</p>
                    {detailLine && (
                      <p className="mt-1 text-sm text-muted-foreground">{detailLine}</p>
                    )}
                    <p className="mt-1 text-xs text-muted-foreground">
                      {getAuditEventTypeLabel(event.eventType)} · {new Date(event.occurredAtUtc).toLocaleString()}
                    </p>
                  </div>
                );
              })}
            </div>
          </section>
          <aside className="rounded-lg border border-border bg-card p-5">
            <h2 className="mb-3 text-sm font-medium text-foreground">Ranking final</h2>
            <ol className="space-y-2">
              {detail.ranking.map((entry, index) => <li key={entry.teamId} className="flex justify-between text-sm"><span>{index + 1}. {entry.teamName}</span><span className="font-medium">{entry.totalScore} pts</span></li>)}
            </ol>
          </aside>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">Historial de auditoría</h1>
      <p className="mt-1 text-sm text-muted-foreground">Sesiones finalizadas disponibles para consulta.</p>
      {error && <p className="mt-6 text-sm text-destructive">{error}</p>}
      <div className="mt-6 space-y-3">
        {sessions.map((session) => (
          <button key={session.sessionId} className="w-full rounded-lg border border-border bg-card p-4 text-left hover:bg-accent/40" onClick={() => void openDetail(session.sessionId)}>
            <p className="font-medium text-foreground">Sesión {session.sessionId.slice(0, 8)}…</p>
            <p className="mt-1 text-sm text-muted-foreground">{session.status} · iniciada {new Date(session.startedAtUtc).toLocaleString()}</p>
          </button>
        ))}
        {!error && sessions.length === 0 && <p className="text-sm text-muted-foreground">No hay sesiones finalizadas.</p>}
      </div>
    </div>
  );
}
