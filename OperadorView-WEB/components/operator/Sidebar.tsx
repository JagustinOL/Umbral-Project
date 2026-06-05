'use client';

import { cn } from '@/lib/utils';
import { RadioIcon, ShieldIcon } from 'lucide-react';
import { ViewType } from './OperatorDashboard';

interface OperatorProfile {
  operatorId: string;
  displayName: string;
  email: string;
}

interface SidebarProps {
  currentView: ViewType;
  onNavigate: (view: ViewType) => void;
  operatorProfile: OperatorProfile | null;
}

export function Sidebar({ currentView, onNavigate, operatorProfile }: SidebarProps) {
  const initials = operatorProfile?.displayName
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase() || 'OP';

  return (
    <aside className="flex flex-col w-56 shrink-0 h-screen bg-sidebar border-r border-sidebar-border sticky top-0">
      <div className="flex items-center gap-2.5 px-5 py-5 border-b border-sidebar-border">
        <div className="h-7 w-7 rounded-md bg-sidebar-primary flex items-center justify-center shrink-0">
          <ShieldIcon className="h-4 w-4 text-sidebar-primary-foreground" />
        </div>
        <div>
          <p className="text-sm font-bold text-sidebar-primary leading-none tracking-wide">UMBRAL</p>
          <p className="text-xs text-sidebar-foreground/50 mt-0.5">Operator Console</p>
        </div>
      </div>

      <div className="px-5 pt-5 pb-2">
        <p className="text-xs font-medium text-sidebar-foreground/40 uppercase tracking-wider">
          Navigation
        </p>
      </div>

      <nav className="flex-1 px-3 space-y-0.5">
        <button
          onClick={() => onNavigate('missions')}
          className={cn(
            'w-full flex items-center gap-3 px-3 py-2.5 rounded-md text-left transition-colors',
            currentView === 'missions'
              ? 'bg-sidebar-accent text-sidebar-accent-foreground'
              : 'text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground',
          )}
        >
          <RadioIcon className="h-4 w-4 shrink-0" />
          <div className="min-w-0">
            <p className="text-sm font-medium leading-none">Missions</p>
            <p className="text-xs text-sidebar-foreground/40 mt-0.5 leading-none">Assigned & sessions</p>
          </div>
          {currentView === 'missions' && (
            <span className="ml-auto h-1.5 w-1.5 rounded-full bg-sidebar-primary shrink-0" />
          )}
        </button>
      </nav>

      <div className="px-5 py-4 border-t border-sidebar-border">
        <div className="flex items-center gap-2.5">
          <div className="h-7 w-7 rounded-full bg-sidebar-accent flex items-center justify-center shrink-0">
            <span className="text-xs font-semibold text-sidebar-accent-foreground">{initials}</span>
          </div>
          <div className="min-w-0">
            <p className="text-xs font-medium text-sidebar-foreground leading-none truncate">
              {operatorProfile?.displayName ?? 'Operador'}
            </p>
            {operatorProfile?.email && (
              <p className="text-xs text-sidebar-foreground/40 truncate mt-0.5">
                {operatorProfile.email}
              </p>
            )}
          </div>
        </div>
      </div>
    </aside>
  );
}
