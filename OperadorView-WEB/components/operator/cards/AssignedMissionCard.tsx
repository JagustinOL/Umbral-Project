'use client';

import { PlayIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { OperatorAssignedMissionDto } from '@/lib/types/api';

interface AssignedMissionCardProps {
  mission: OperatorAssignedMissionDto;
  hasOpenSession: boolean;
  isCreating: boolean;
  disabled: boolean;
  onCreateSession: (missionId: string, missionTitle: string) => Promise<void>;
}

export function AssignedMissionCard({
  mission,
  hasOpenSession,
  isCreating,
  disabled,
  onCreateSession,
}: AssignedMissionCardProps) {
  const handleCreateSession = () => {
    void onCreateSession(mission.missionId, mission.title);
  };

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
          Esta misión ya tiene una sesión abierta. Debe finalizarla antes de crear otra.
        </p>
      )}

      <Button
        onClick={handleCreateSession}
        disabled={disabled || hasOpenSession || isCreating}
        className="w-full gap-2 mt-auto"
      >
        <PlayIcon className="h-4 w-4" />
        {isCreating ? 'Creando sesión…' : 'Crear sesión en vivo'}
      </Button>
    </div>
  );
}
