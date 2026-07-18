"use client";

import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeftIcon,
  FlagIcon,
  MedalIcon,
  TrophyIcon,
  UsersIcon,
} from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  XAxis,
  YAxis,
} from "recharts";
import { Button } from "@/components/ui/button";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { ApiError, scoringApiRequest } from "@/lib/api/client";
import { getAuditEventDetail, getAuditEventTypeLabel } from "@/lib/auditDisplay";
import { missionService } from "@/lib/services/missionService";
import { operatorService } from "@/lib/services/operatorService";
import { cn } from "@/lib/utils";

type HistoricalSession = {
  sessionId: string;
  missionId: string;
  operatorId: string;
  startedAtUtc: string;
  endedAtUtc: string | null;
  status: string;
};

type AuditDetail = {
  sessionId: string;
  status: string;
  startedAtUtc: string;
  endedAtUtc: string | null;
  timeline: {
    eventId: string;
    eventType: string | number;
    occurredAtUtc: string;
    description: string;
    teamId?: string | null;
    missionNodeId?: string | null;
    metadata?: string | null;
  }[];
  ranking: {
    position?: number;
    teamId: string;
    teamName: string;
    totalScore: number;
    completedNodes?: number;
    lastElapsedSeconds?: number;
  }[];
};

type AuditDashboard = {
  generalRanking: {
    position: number;
    teamId: string;
    teamName: string;
    totalScore: number;
    elapsedSeconds: number;
    sessionId: string;
    missionId: string;
  }[];
  topMissions: { missionId: string; sessionCount: number }[];
  topOperators: { operatorId: string; sessionCount: number }[];
  totalFinishedSessions: number;
};

const CHART_COLORS = [
  "var(--color-chart-1)",
  "var(--color-chart-2)",
  "var(--color-chart-3)",
  "var(--color-chart-4)",
  "var(--color-chart-5)",
];

const missionChartConfig = {
  sessions: { label: "Sesiones", color: "var(--chart-2)" },
} satisfies ChartConfig;

const operatorChartConfig = {
  sessions: { label: "Sesiones", color: "var(--chart-3)" },
} satisfies ChartConfig;

function getErrorMessage(error: unknown) {
  return error instanceof ApiError ? error.message : "No se pudo cargar la auditoría.";
}

function truncateId(id: string) {
  return `${id.slice(0, 8)}…`;
}

function shortLabel(value: string, max = 14) {
  return value.length > max ? `${value.slice(0, max - 1)}…` : value;
}

function KpiCard({
  label,
  value,
  hint,
  icon: Icon,
  accent,
}: {
  label: string;
  value: string | number;
  hint: string;
  icon: typeof TrophyIcon;
  accent: string;
}) {
  return (
    <div className="relative overflow-hidden rounded-xl border border-border bg-card p-5 shadow-sm transition-transform duration-300 hover:-translate-y-0.5">
      <div
        className={cn("pointer-events-none absolute -right-6 -top-6 h-24 w-24 rounded-full opacity-20 blur-2xl", accent)}
      />
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">{label}</p>
          <p className="mt-2 text-3xl font-semibold tracking-tight text-foreground tabular-nums">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{hint}</p>
        </div>
        <div className="rounded-lg bg-foreground/5 p-2.5 text-foreground/80">
          <Icon className="h-5 w-5" />
        </div>
      </div>
    </div>
  );
}

function Podium({
  entries,
  missionLabel,
}: {
  entries: AuditDashboard["generalRanking"];
  missionLabel: (id: string) => string;
}) {
  const top = [entries[1], entries[0], entries[2]].filter(Boolean);
  const heights = ["h-14", "h-20", "h-12"];
  const medals = ["bg-zinc-300 text-zinc-800", "bg-amber-400 text-amber-950", "bg-orange-400 text-orange-950"];
  const labels = ["2º", "1º", "3º"];

  if (entries.length === 0) return null;

  const ordered =
    entries.length === 1
      ? [{ entry: entries[0], height: "h-20", medal: medals[1], label: "1º" }]
      : entries.length === 2
        ? [
            { entry: entries[1], height: "h-14", medal: medals[0], label: "2º" },
            { entry: entries[0], height: "h-20", medal: medals[1], label: "1º" },
          ]
        : top.map((entry, i) => ({
            entry,
            height: heights[i],
            medal: medals[i],
            label: labels[i],
          }));

  return (
    <div className="flex items-end justify-center gap-2 px-1 pb-1 pt-3">
      {ordered.map(({ entry, height, medal, label }) => (
        <div key={`${entry.sessionId}-${entry.teamId}`} className="flex w-20 flex-col items-center sm:w-24">
          <div className="mb-1.5 text-center">
            <p className="truncate text-xs font-semibold text-foreground">{entry.teamName}</p>
            <p className="text-[11px] text-muted-foreground">{entry.totalScore} pts</p>
            <p className="truncate text-[10px] text-muted-foreground">{missionLabel(entry.missionId)}</p>
          </div>
          <div
            className={cn(
              "flex w-full flex-col items-center justify-end rounded-t-lg border border-border/60 bg-gradient-to-t from-muted/80 to-muted/20 shadow-inner transition-all duration-500",
              height,
            )}
          >
            <span className={cn("mb-2 inline-flex h-6 w-6 items-center justify-center rounded-full text-[10px] font-bold shadow", medal)}>
              {label}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}

export function AuditHistory() {
  const [sessions, setSessions] = useState<HistoricalSession[]>([]);
  const [dashboard, setDashboard] = useState<AuditDashboard | null>(null);
  const [missionTitles, setMissionTitles] = useState<Record<string, string>>({});
  const [operatorNames, setOperatorNames] = useState<Record<string, string>>({});
  const [detail, setDetail] = useState<AuditDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void Promise.all([
      scoringApiRequest<{ items: HistoricalSession[] }>("/audit/sessions"),
      scoringApiRequest<AuditDashboard>("/audit/dashboard"),
    ])
      .then(([sessionsResult, dashboardResult]) => {
        setSessions(sessionsResult.items);
        setDashboard(dashboardResult);
      })
      .catch((requestError) => setError(getErrorMessage(requestError)));

    void missionService
      .getMissions()
      .then((missions) => {
        const map: Record<string, string> = {};
        for (const mission of missions) map[mission.id] = mission.title;
        setMissionTitles(map);
      })
      .catch(() => undefined);

    void operatorService
      .getOperators()
      .then((operators) => {
        const map: Record<string, string> = {};
        for (const op of operators) {
          map[op.operatorId] = `${op.firstName} ${op.lastName}`.trim();
        }
        setOperatorNames(map);
      })
      .catch(() => undefined);
  }, []);

  const missionLabel = useMemo(
    () => (missionId: string) => missionTitles[missionId] ?? truncateId(missionId),
    [missionTitles],
  );

  const operatorLabel = useMemo(
    () => (operatorId: string) => operatorNames[operatorId] ?? truncateId(operatorId),
    [operatorNames],
  );

  const missionChartData = useMemo(
    () =>
      (dashboard?.topMissions ?? []).map((item) => ({
        name: shortLabel(missionLabel(item.missionId), 16),
        fullName: missionLabel(item.missionId),
        sessions: item.sessionCount,
      })),
    [dashboard, missionLabel],
  );

  const operatorChartData = useMemo(
    () =>
      (dashboard?.topOperators ?? []).map((item) => ({
        name: shortLabel(operatorLabel(item.operatorId), 12),
        fullName: operatorLabel(item.operatorId),
        sessions: item.sessionCount,
      })),
    [dashboard, operatorLabel],
  );

  const topScore = dashboard?.generalRanking[0]?.totalScore ?? 0;
  const topTeam = dashboard?.generalRanking[0]?.teamName ?? "—";

  const selectSession = async (sessionId: string) => {
    try {
      setError(null);
      setDetail(await scoringApiRequest<AuditDetail>(`/audit/sessions/${sessionId}`));
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    }
  };

  if (detail) {
    return (
      <div>
        <div className="mb-8 flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => setDetail(null)}>
            <ArrowLeftIcon className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-foreground">Detalle de auditoría</h1>
            <p className="mt-1 text-sm text-muted-foreground">{detail.status}</p>
          </div>
        </div>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <section className="space-y-3 lg:col-span-2">
            <h2 className="text-sm font-medium text-foreground">Línea de tiempo</h2>
            {detail.timeline.map((event) => {
              const detailLine = getAuditEventDetail(event.metadata, event.missionNodeId);
              return (
                <div key={event.eventId} className="rounded-lg border border-border bg-card p-4">
                  <p className="font-medium text-foreground">{event.description}</p>
                  {detailLine && (
                    <p className="mt-1 text-sm text-muted-foreground">{detailLine}</p>
                  )}
                  <p className="mt-1 text-xs text-muted-foreground">
                    {getAuditEventTypeLabel(event.eventType)} ·{" "}
                    {new Date(event.occurredAtUtc).toLocaleString()}
                  </p>
                </div>
              );
            })}
          </section>
          <aside className="rounded-lg border border-border bg-card p-5">
            <h2 className="mb-3 text-sm font-medium text-foreground">Ranking final</h2>
            <ol className="space-y-2">
              {detail.ranking.map((entry, index) => (
                <li key={entry.teamId} className="flex justify-between text-sm">
                  <span>
                    {index + 1}. {entry.teamName}
                  </span>
                  <span className="font-medium">{entry.totalScore} pts</span>
                </li>
              ))}
            </ol>
          </aside>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="relative overflow-hidden rounded-2xl border border-border bg-gradient-to-br from-card via-card to-muted/40 p-6 sm:p-8">
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,oklch(0.7_0.12_70_/0.12),transparent_55%)]" />
        <div className="relative">
          <p className="text-xs font-medium uppercase tracking-[0.2em] text-muted-foreground">Panel de auditoría</p>
          <h1 className="mt-2 text-3xl font-semibold tracking-tight text-foreground">Rendimiento del sistema</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Ranking histórico, misiones más jugadas y carga de operadores en un vistazo.
          </p>
        </div>
      </div>

      {error && <p className="mt-6 text-sm text-destructive">{error}</p>}

      {dashboard && (
        <>
          <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <KpiCard
              label="Sesiones finalizadas"
              value={dashboard.totalFinishedSessions}
              hint="Solo estados Finished"
              icon={FlagIcon}
              accent="bg-emerald-500"
            />
            <KpiCard
              label="Mejor puntaje"
              value={topScore}
              hint={topTeam}
              icon={TrophyIcon}
              accent="bg-amber-500"
            />
            <KpiCard
              label="Misiones activas en ranking"
              value={dashboard.topMissions.length}
              hint="Con al menos una sesión"
              icon={MedalIcon}
              accent="bg-sky-500"
            />
            <KpiCard
              label="Operadores activos"
              value={dashboard.topOperators.length}
              hint="Con sesiones cerradas"
              icon={UsersIcon}
              accent="bg-orange-500"
            />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-3">
            <section className="rounded-xl border border-border bg-card p-4 shadow-sm lg:col-span-1">
              <div className="mb-1 flex items-center gap-2">
                <TrophyIcon className="h-3.5 w-3.5 text-amber-600" />
                <h2 className="text-sm font-medium text-foreground">Podio histórico</h2>
              </div>
              <p className="mb-1 text-xs text-muted-foreground">Top 3 por mejor puntuación</p>
              {dashboard.generalRanking.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">Sin puntuaciones aún.</p>
              ) : (
                <Podium entries={dashboard.generalRanking} missionLabel={missionLabel} />
              )}
            </section>

            <section className="rounded-xl border border-border bg-card p-5 shadow-sm lg:col-span-1">
              <h2 className="text-sm font-medium text-foreground">Misiones más jugadas</h2>
              <p className="mb-4 text-xs text-muted-foreground">Distribución de sesiones por misión</p>
              {missionChartData.length === 0 ? (
                <p className="py-10 text-center text-sm text-muted-foreground">Sin datos.</p>
              ) : (
                <div className="grid grid-cols-1 items-center gap-4 sm:grid-cols-[minmax(0,1fr)_140px]">
                  <div className="mx-auto h-[210px] w-full max-w-[210px]">
                    <ChartContainer
                      config={missionChartConfig}
                      className="!aspect-auto h-full w-full [&_.recharts-pie-sector]:outline-none"
                    >
                      <PieChart margin={{ top: 4, right: 4, bottom: 4, left: 4 }}>
                        <ChartTooltip
                          content={
                            <ChartTooltipContent
                              nameKey="fullName"
                              formatter={(value) => (
                                <span className="font-medium">{value as number} sesiones</span>
                              )}
                            />
                          }
                        />
                        <Pie
                          data={missionChartData}
                          dataKey="sessions"
                          nameKey="name"
                          cx="50%"
                          cy="50%"
                          innerRadius="55%"
                          outerRadius="90%"
                          paddingAngle={2}
                          stroke="var(--card)"
                          strokeWidth={3}
                          isAnimationActive={false}
                        >
                          {missionChartData.map((_, index) => (
                            <Cell
                              key={index}
                              fill={CHART_COLORS[index % CHART_COLORS.length]}
                              stroke="var(--card)"
                              strokeWidth={3}
                            />
                          ))}
                        </Pie>
                      </PieChart>
                    </ChartContainer>
                  </div>
                  <ul className="space-y-2">
                    {missionChartData.map((item, index) => (
                      <li key={item.fullName} className="flex items-center gap-2 text-xs">
                        <span
                          className="h-2.5 w-2.5 shrink-0 rounded-full"
                          style={{ background: CHART_COLORS[index % CHART_COLORS.length] }}
                        />
                        <span className="min-w-0 flex-1 truncate text-muted-foreground" title={item.fullName}>
                          {item.fullName}
                        </span>
                        <span className="font-medium tabular-nums text-foreground">{item.sessions}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </section>

            <section className="rounded-xl border border-border bg-card p-5 shadow-sm lg:col-span-1">
              <h2 className="text-sm font-medium text-foreground">Operadores con más sesiones</h2>
              <p className="mb-4 text-xs text-muted-foreground">Carga operativa por operador</p>
              {operatorChartData.length === 0 ? (
                <p className="py-10 text-center text-sm text-muted-foreground">Sin datos.</p>
              ) : (
                <ChartContainer config={operatorChartConfig} className="aspect-[16/10] w-full">
                  <BarChart data={operatorChartData} margin={{ left: 4, right: 8, top: 8 }}>
                    <CartesianGrid vertical={false} strokeDasharray="3 3" />
                    <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fontSize: 11 }} />
                    <YAxis allowDecimals={false} tickLine={false} axisLine={false} width={28} />
                    <ChartTooltip
                      content={
                        <ChartTooltipContent
                          labelFormatter={(_, payload) =>
                            String(payload?.[0]?.payload?.fullName ?? "")
                          }
                          formatter={(value) => (
                            <span className="font-medium">{value as number} sesiones</span>
                          )}
                        />
                      }
                    />
                    <Bar dataKey="sessions" radius={[6, 6, 0, 0]} fill="var(--color-sessions)" maxBarSize={36}>
                      {operatorChartData.map((_, index) => (
                        <Cell key={index} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                      ))}
                    </Bar>
                  </BarChart>
                </ChartContainer>
              )}
            </section>
          </div>
        </>
      )}

      <h2 className="mt-10 text-lg font-medium tracking-tight text-foreground">Historial de sesiones</h2>
      <p className="mt-1 text-sm text-muted-foreground">Consulta el detalle y el ranking final de cada sesión.</p>
      <div className="mt-4 space-y-3">
        {sessions.map((session) => (
          <button
            key={session.sessionId}
            type="button"
            className="group w-full rounded-xl border border-border bg-card p-4 text-left shadow-sm transition-all hover:-translate-y-0.5 hover:border-foreground/20 hover:shadow-md"
            onClick={() => void selectSession(session.sessionId)}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-medium text-foreground">
                  {missionLabel(session.missionId)}
                </p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Sesión {truncateId(session.sessionId)} · {operatorLabel(session.operatorId)} ·{" "}
                  {new Date(session.startedAtUtc).toLocaleString()}
                </p>
              </div>
              <span
                className={cn(
                  "shrink-0 rounded-full px-2.5 py-1 text-[11px] font-medium uppercase tracking-wide",
                  session.status === "Finished"
                    ? "bg-emerald-500/15 text-emerald-700"
                    : "bg-muted text-muted-foreground",
                )}
              >
                {session.status}
              </span>
            </div>
          </button>
        ))}
        {!error && sessions.length === 0 && (
          <p className="text-sm text-muted-foreground">No hay sesiones finalizadas.</p>
        )}
      </div>
    </div>
  );
}
