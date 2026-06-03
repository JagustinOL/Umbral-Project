'use client';

import { Check, Users, Clock } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

interface Team {
  id: string;
  name: string;
  memberCount: number;
  joinedAt: string;
  status: 'pending' | 'approved' | 'rejected';
}

interface TeamsListProps {
  teams: Team[];
}

export function TeamsList({ teams }: TeamsListProps) {
  if (teams.length === 0) {
    return (
      <div className="p-8 text-center border border-dashed border-slate-700 rounded-lg bg-slate-900/50">
        <Users className="w-8 h-8 text-slate-500 mx-auto mb-2" />
        <p className="text-slate-400">No teams have joined yet</p>
        <p className="text-sm text-slate-500 mt-1">Share the join code to invite teams</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {teams.map((team) => (
        <div
          key={team.id}
          className="p-4 bg-slate-700/50 border border-slate-700 rounded-lg hover:border-slate-600 transition-colors"
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <h4 className="font-semibold text-slate-50">{team.name}</h4>
                <Badge variant="outline" className="text-xs bg-green-950 border-green-800 text-green-200">
                  <Check className="w-3 h-3 mr-1" />
                  Approved
                </Badge>
              </div>
              <div className="flex items-center gap-3 text-sm text-slate-400">
                <span className="flex items-center gap-1">
                  <Users className="w-3 h-3" />
                  {team.memberCount} member{team.memberCount !== 1 ? 's' : ''}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  {team.joinedAt}
                </span>
              </div>
            </div>
            <div className="ml-2 h-2 w-2 bg-green-500 rounded-full"></div>
          </div>
        </div>
      ))}
    </div>
  );
}
