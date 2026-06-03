'use client';

import { Zap, Radio, User, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ViewType } from './OperatorDashboard';

interface SidebarProps {
  currentView: ViewType;
  onNavigate: (view: ViewType) => void;
}

export function Sidebar({ currentView, onNavigate }: SidebarProps) {
  return (
    <aside className="w-64 bg-slate-900 border-r border-slate-800 flex flex-col">
      {/* Header */}
      <div className="p-6 border-b border-slate-800">
        <div className="flex items-center gap-2 mb-1">
          <Zap className="w-6 h-6 text-amber-400" />
          <h1 className="text-xl font-bold text-slate-50">UMBRAL</h1>
        </div>
        <p className="text-xs text-slate-400">Operator Control Room</p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-2">
        <Button
          onClick={() => onNavigate('missions')}
          variant={currentView === 'missions' ? 'default' : 'ghost'}
          className={`w-full justify-start gap-2 ${
            currentView === 'missions'
              ? 'bg-amber-600 hover:bg-amber-700'
              : 'text-slate-300 hover:text-slate-50'
          }`}
        >
          <Radio className="w-4 h-4" />
          <span>Assigned Missions</span>
        </Button>
      </nav>

      {/* User Section */}
      <div className="p-4 border-t border-slate-800">
        <div className="flex items-center gap-3 px-3 py-2 rounded-lg bg-slate-800/50 mb-3">
          <User className="w-4 h-4 text-slate-400" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-slate-50 truncate">Operator One</p>
            <p className="text-xs text-slate-400 truncate">op-001@umbral.io</p>
          </div>
        </div>
        <Button
          variant="ghost"
          className="w-full justify-start gap-2 text-slate-400 hover:text-slate-50"
        >
          <LogOut className="w-4 h-4" />
          <span>Sign Out</span>
        </Button>
      </div>
    </aside>
  );
}
