/** Etiquetas amigables para SessionEventType (nombre o valor numérico). */
const EVENT_TYPE_LABELS: Record<string, string> = {
  SessionStarted: "Sesión iniciada",
  "0": "Sesión iniciada",
  SessionPaused: "Sesión pausada",
  "1": "Sesión pausada",
  SessionResumed: "Sesión reanudada",
  "2": "Sesión reanudada",
  SessionFinalized: "Sesión finalizada",
  "3": "Sesión finalizada",
  SessionCancelled: "Sesión cancelada",
  "4": "Sesión cancelada",
  EvidenceValidated: "Evidencia validada",
  "5": "Evidencia validada",
  EvidenceRejected: "Evidencia rechazada",
  "6": "Evidencia rechazada",
  HintReleased: "Pista liberada",
  "7": "Pista liberada",
  ManualPenaltyApplied: "Penalización manual",
  "8": "Penalización manual",
  TeamRegistered: "Equipo registrado",
  "9": "Equipo registrado",
  ScoreRecalculated: "Puntaje recalculado",
  "10": "Puntaje recalculado",
  TeamCompletedMission: "Misión completada",
  "11": "Misión completada",
};

export type AuditEventMetadata = {
  detail?: string;
  teamName?: string;
  nodeType?: string;
  nodeTitle?: string;
  score?: number;
  penaltyPoints?: number;
  reason?: string;
  hintOrder?: number;
  wasManualRelease?: boolean;
  elapsedSeconds?: number;
  teamCount?: number;
};

export function getAuditEventTypeLabel(eventType: string | number): string {
  const key = String(eventType);
  return EVENT_TYPE_LABELS[key] ?? "Evento de sesión";
}

export function parseAuditMetadata(metadata: string | null | undefined): AuditEventMetadata | null {
  if (!metadata) return null;
  try {
    return JSON.parse(metadata) as AuditEventMetadata;
  } catch {
    return null;
  }
}

function formatElapsed(seconds: number): string {
  const total = Math.max(0, Math.round(seconds));
  const minutes = Math.floor(total / 60);
  const secs = total % 60;
  return `${minutes}m ${String(secs).padStart(2, "0")}s`;
}

function formatNodeLabel(meta: AuditEventMetadata, missionNodeId?: string | null): string | null {
  const typeLabel =
    meta.nodeType === "TreasureHunt"
      ? "Búsqueda del tesoro"
      : meta.nodeType === "Trivia"
        ? "Trivia"
        : meta.nodeType || null;

  if (meta.nodeTitle && typeLabel) return `${typeLabel} "${meta.nodeTitle}"`;
  if (meta.nodeTitle) return `"${meta.nodeTitle}"`;
  if (typeLabel && missionNodeId) return `${typeLabel} ${missionNodeId.slice(0, 8)}`;
  return typeLabel;
}

/** Detalle secundario: usa metadata.detail si existe; si no, reconstruye desde campos conocidos. */
export function getAuditEventDetail(
  metadata: string | null | undefined,
  missionNodeId?: string | null,
): string | null {
  const meta = parseAuditMetadata(metadata);
  if (!meta) return null;
  if (meta.detail) return meta.detail;

  const parts: string[] = [];
  const nodeLabel = formatNodeLabel(meta, missionNodeId);
  if (nodeLabel) parts.push(nodeLabel);

  if (typeof meta.score === "number") parts.push(`+${meta.score} pts`);
  if (typeof meta.penaltyPoints === "number" && meta.penaltyPoints > 0) {
    parts.push(`-${meta.penaltyPoints} pts`);
  }
  if (meta.reason) parts.push(`Motivo: ${meta.reason}`);
  if (typeof meta.hintOrder === "number" && meta.hintOrder > 0) {
    parts.push(`pista #${meta.hintOrder}`);
  }
  if (typeof meta.wasManualRelease === "boolean") {
    parts.push(meta.wasManualRelease ? "Manual" : "Automática");
  }
  if (typeof meta.elapsedSeconds === "number") {
    parts.push(`tiempo ${formatElapsed(meta.elapsedSeconds)}`);
  }
  if (typeof meta.teamCount === "number") {
    parts.push(`${meta.teamCount} ${meta.teamCount === 1 ? "equipo" : "equipos"}`);
  }

  return parts.length > 0 ? parts.join(" · ") : null;
}
