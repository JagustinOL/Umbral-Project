import type { RankingEntry } from '../types/gameplay';

export type RankingEvent = {
  id: string;
  message: string;
  tone: 'up' | 'down' | 'same' | 'info';
  teamId: string;
  createdAt: number;
};

function ordinal(position: number): string {
  return `${position}.º`;
}

/**
 * Compara ranking anterior vs nuevo y genera mensajes narrativos de cambio de puesto.
 */
export function buildRankingEvents(
  previous: RankingEntry[],
  next: RankingEntry[],
  ownTeamId?: string | null,
): RankingEvent[] {
  if (next.length === 0) {
    return [];
  }

  const prevById = new Map(
    previous.map((entry) => [entry.teamId.toLowerCase(), entry]),
  );
  const now = Date.now();
  const events: RankingEvent[] = [];
  const ownId = ownTeamId?.toLowerCase() ?? null;

  for (const entry of next) {
    const key = entry.teamId.toLowerCase();
    const before = prevById.get(key);
    const isYou = ownId !== null && key === ownId;
    const label = isYou ? 'Tu equipo' : entry.teamName;

    if (!before) {
      events.push({
        id: `${key}-join-${now}-${entry.position}`,
        message: `${label} entra al ranking en el ${ordinal(entry.position)} puesto (${entry.totalScore} pts).`,
        tone: 'info',
        teamId: entry.teamId,
        createdAt: now,
      });
      continue;
    }

    if (before.position === entry.position && before.totalScore === entry.totalScore) {
      continue;
    }

    if (entry.position < before.position) {
      events.push({
        id: `${key}-up-${now}-${entry.position}`,
        message: `${label} subió al ${ordinal(entry.position)} puesto (${entry.totalScore} pts).`,
        tone: 'up',
        teamId: entry.teamId,
        createdAt: now,
      });
      continue;
    }

    if (entry.position > before.position) {
      events.push({
        id: `${key}-down-${now}-${entry.position}`,
        message: `${label} bajó al ${ordinal(entry.position)} puesto (${entry.totalScore} pts).`,
        tone: 'down',
        teamId: entry.teamId,
        createdAt: now,
      });
      continue;
    }

    // Misma posición, cambió puntaje.
    const delta = entry.totalScore - before.totalScore;
    const deltaLabel = delta >= 0 ? `+${delta}` : `${delta}`;
    events.push({
      id: `${key}-score-${now}-${entry.totalScore}`,
      message: `${label} se mantiene en el ${ordinal(entry.position)} puesto (${deltaLabel} pts → ${entry.totalScore}).`,
      tone: 'same',
      teamId: entry.teamId,
      createdAt: now,
    });
  }

  // Podio corto cuando hay movimiento relevante.
  if (events.length > 0 && next.length >= 1) {
    const podium = next
      .slice(0, Math.min(3, next.length))
      .map((e) => `${ordinal(e.position)} ${e.teamName}`)
      .join(' · ');
    events.push({
      id: `podium-${now}`,
      message: `Podio actual: ${podium}`,
      tone: 'info',
      teamId: next[0]?.teamId ?? '',
      createdAt: now,
    });
  }

  return events;
}
