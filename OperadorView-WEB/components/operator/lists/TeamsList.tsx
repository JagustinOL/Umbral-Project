'use client';

import { UsersIcon } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

interface TeamsListProps {
  teamIds: string[];
  isLoading: boolean;
}

export function TeamsList({ teamIds, isLoading }: TeamsListProps) {
  if (isLoading && teamIds.length === 0) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-16 w-full rounded-lg" />
        <Skeleton className="h-16 w-full rounded-lg" />
      </div>
    );
  }

  if (teamIds.length === 0) {
    return (
      <div className="p-8 text-center border border-dashed border-border rounded-lg bg-muted/30">
        <UsersIcon className="w-8 h-8 text-muted-foreground mx-auto mb-2" />
        <p className="text-sm text-muted-foreground">Aún no hay equipos registrados</p>
        <p className="text-xs text-muted-foreground/70 mt-1">
          Comparta el código de unión para que los equipos se incorporen
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {teamIds.map((teamId) => (
        <div
          key={teamId}
          className="flex items-center justify-between rounded-lg border border-border bg-card px-4 py-3"
        >
          <div className="flex items-center gap-3 min-w-0">
            <div className="h-8 w-8 rounded-full bg-secondary flex items-center justify-center shrink-0">
              <UsersIcon className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-medium text-foreground truncate">Equipo registrado</p>
              <p className="text-xs text-muted-foreground font-mono truncate">{teamId}</p>
            </div>
          </div>
          <span className="h-2 w-2 rounded-full bg-status-active shrink-0" />
        </div>
      ))}
    </div>
  );
}
