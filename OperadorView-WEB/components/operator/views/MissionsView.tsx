'use client';

import { AssignedMissionCard } from '../cards/AssignedMissionCard';
import { InfoAlert } from '../ui/InfoAlert';
import { getMockMissions } from '@/lib/mockData';

interface MissionsViewProps {
  onMissionCreate: (sessionId: string, missionId: string, missionTitle: string) => void;
}

export function MissionsView({ onMissionCreate }: MissionsViewProps) {
  const missions = getMockMissions();

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-3xl font-bold text-slate-50 mb-2">Assigned Missions</h2>
        <p className="text-slate-400">Manage and initiate live gaming sessions</p>
      </div>

      {/* Business Rule Notice - RN-16 */}
      <InfoAlert
        title="Access Restriction (RN-16)"
        description="You can only view and operate missions explicitly assigned to your account. Contact your administrator to request access to additional missions."
      />

      {/* Missions Grid */}
      <div className="mt-8 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {missions.map((mission) => (
          <AssignedMissionCard
            key={mission.id}
            mission={mission}
            onCreateSession={onMissionCreate}
          />
        ))}
      </div>

      {/* Empty State */}
      {missions.length === 0 && (
        <div className="mt-12 text-center py-12 border-2 border-dashed border-slate-700 rounded-lg">
          <p className="text-slate-400">No missions assigned yet</p>
          <p className="text-sm text-slate-500 mt-1">
            Check back soon or contact your administrator
          </p>
        </div>
      )}
    </div>
  );
}
