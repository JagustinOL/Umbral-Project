'use client';

import { Play, Zap } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useState } from 'react';

interface Mission {
  id: string;
  title: string;
  description: string;
  difficulty: number;
  maxDurationMinutes?: number;
  stageCount: number;
}

interface AssignedMissionCardProps {
  mission: Mission;
  onCreateSession: (sessionId: string, missionId: string, missionTitle: string) => void;
}

export function AssignedMissionCard({ mission, onCreateSession }: AssignedMissionCardProps) {
  const [isCreating, setIsCreating] = useState(false);

  const handleCreateSession = () => {
    setIsCreating(true);
    // Simulate API call
    setTimeout(() => {
      const sessionId = `session-${Date.now()}`;
      onCreateSession(sessionId, mission.id, mission.title);
      setIsCreating(false);
    }, 800);
  };

  return (
    <div className="p-6 bg-slate-800 border border-slate-700 rounded-lg hover:border-slate-600 transition-all hover:shadow-lg hover:shadow-amber-900/20">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-slate-50 mb-1">{mission.title}</h3>
          <p className="text-sm text-slate-400">{mission.description}</p>
        </div>
        <div className="ml-2 px-2 py-1 bg-amber-900/40 border border-amber-700 rounded text-xs font-medium text-amber-200">
          Assigned
        </div>
      </div>

      {/* Mission Meta */}
      <div className="grid grid-cols-3 gap-2 mb-6 py-4 border-y border-slate-700">
        <div>
          <p className="text-xs text-slate-500 uppercase tracking-wide">Difficulty</p>
          <p className="text-sm font-semibold text-slate-50">{mission.difficulty}/10</p>
        </div>
        <div>
          <p className="text-xs text-slate-500 uppercase tracking-wide">Stages</p>
          <p className="text-sm font-semibold text-slate-50">{mission.stageCount}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500 uppercase tracking-wide">Duration</p>
          <p className="text-sm font-semibold text-slate-50">
            {mission.maxDurationMinutes || '—'} min
          </p>
        </div>
      </div>

      {/* Action Button - HU-48 */}
      <Button
        onClick={handleCreateSession}
        disabled={isCreating}
        className="w-full gap-2 bg-amber-600 hover:bg-amber-700 text-slate-50"
      >
        <Play className="w-4 h-4" />
        {isCreating ? 'Creating Session...' : 'Create Live Session'}
      </Button>
    </div>
  );
}
