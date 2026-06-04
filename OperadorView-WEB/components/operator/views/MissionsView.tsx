'use client';

import { AssignedMissionCard } from '../cards/AssignedMissionCard';
import { InfoAlert } from '../ui/InfoAlert';
import { OperatorAssignedMissionDto } from '@/lib/types/api';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { AlertTriangleIcon } from 'lucide-react';

interface MissionsViewProps {
  missions: OperatorAssignedMissionDto[];
  openSessionByMission: Record<string, boolean>;
  isLoading: boolean;
  isCreating: boolean;
  creatingMissionId: string | null;
  errorMessage: string | null;
  onRetry: () => void;
  onCreateSession: (missionId: string, missionTitle: string) => Promise<void>;
}

export function MissionsView({
  missions,
  openSessionByMission,
  isLoading,
  isCreating,
  creatingMissionId,
  errorMessage,
  onRetry,
  onCreateSession,
}: MissionsViewProps) {
  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Misiones asignadas</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Cree e inicie sesiones en vivo para las misiones bajo su supervisión.
        </p>
      </div>

      <InfoAlert
        title="Acceso restringido"
        description="Solo puede operar misiones asignadas a su cuenta. Contacte al administrador si necesita acceso adicional."
      />

      {errorMessage && (
        <Alert variant="destructive" className="mt-6">
          <AlertTriangleIcon className="h-4 w-4" />
          <AlertTitle>Error al cargar misiones</AlertTitle>
          <AlertDescription className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <span>{errorMessage}</span>
            <Button variant="outline" size="sm" onClick={onRetry}>
              Reintentar
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {isLoading ? (
        <div className="mt-8 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-40 w-full rounded-lg" />
          ))}
        </div>
      ) : (
        <div className="mt-8 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {missions.map((mission) => (
            <AssignedMissionCard
              key={mission.missionId}
              mission={mission}
              hasOpenSession={openSessionByMission[mission.missionId] ?? false}
              isCreating={isCreating && creatingMissionId === mission.missionId}
              disabled={isCreating}
              onCreateSession={onCreateSession}
            />
          ))}
        </div>
      )}

      {!isLoading && !errorMessage && missions.length === 0 && (
        <div className="mt-12 text-center py-12 border border-dashed border-border rounded-lg bg-card">
          <p className="text-muted-foreground">No tiene misiones asignadas</p>
          <p className="text-sm text-muted-foreground/70 mt-1">
            Solicite asignación al administrador del sistema
          </p>
        </div>
      )}
    </div>
  );
}
