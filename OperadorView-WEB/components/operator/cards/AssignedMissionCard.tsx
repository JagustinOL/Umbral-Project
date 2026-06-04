'use client';

import { DoorOpenIcon, PlayIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { OperatorAssignedMissionDto, OperatorOpenSessionDto } from '@/lib/types/api';

interface AssignedMissionCardProps {
  mission: OperatorAssignedMissionDto;
  hasOpenSession: boolean;
  openSession?: OperatorOpenSessionDto;
  isCreating: boolean;
  disabled: boolean;
  onCreateSession: (missionId: string, missionTitle: string) => Promise<void>;
  onOpenSession: (missionId: string, missionTitle: string) => void;
}

export function AssignedMissionCard({
  mission,
  hasOpenSession,
  openSession,
  isCreating,
  disabled,
  onCreateSession,
  onOpenSession,
}: AssignedMissionCardProps) {
  const handleCreateSession = () => {
    void onCreateSession(mission.missionId, mission.title);
  };

  const handleOpenSession = () => {
    onOpenSession(mission.missionId, mission.title);
  };

  const canEnterWaitingRoom = openSession?.status === 'Pending';

  return (
    <div className="rounded-lg border border-border bg-card p-5 flex flex-col gap-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="text-base font-semibold text-card-foreground truncate">{mission.title}</h3>
          <p className="text-xs text-muted-foreground mt-1 font-mono truncate">
            {mission.missionId}
          </p>
        </div>
        <Badge variant="secondary" className="shrink-0">
          Asignada
        </Badge>
      </div>

      {hasOpenSession && (
        <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-md px-3 py-2">
          {canEnterWaitingRoom
            ? 'Hay una sesión pendiente. Puede volver a la sala de espera; no podrá crear otra hasta finalizarla.'
            : `Sesión en curso (${openSession?.status ?? 'activa'}). Finalícela antes de crear una nueva.`}
        </p>
      )}

      {hasOpenSession && canEnterWaitingRoom ? (
        <Button onClick={handleOpenSession} variant="default" className="w-full gap-2 mt-auto">
          <DoorOpenIcon className="h-4 w-4" />
          Ver sala de espera
        </Button>
      ) : hasOpenSession ? null : (
        <Button
          onClick={handleCreateSession}
          disabled={disabled || isCreating}
          className="w-full gap-2 mt-auto"
        >
          <PlayIcon className="h-4 w-4" />
          {isCreating ? 'Creando sesión…' : 'Crear sesión en vivo'}
        </Button>
      )}
    </div>
  );
}
