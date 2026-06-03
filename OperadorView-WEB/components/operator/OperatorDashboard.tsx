'use client';

import { useState } from 'react';
import { Sidebar } from './Sidebar';
import { MissionsView } from './views/MissionsView';
import { WaitingRoomView } from './views/WaitingRoomView';

export type ViewType = 'missions' | 'waiting-room';

interface ActiveSession {
  sessionId: string;
  missionId: string;
  missionTitle: string;
}

export function OperatorDashboard() {
  const [currentView, setCurrentView] = useState<ViewType>('missions');
  const [activeSession, setActiveSession] = useState<ActiveSession | null>(null);

  const handleMissionSelected = (sessionId: string, missionId: string, missionTitle: string) => {
    setActiveSession({ sessionId, missionId, missionTitle });
    setCurrentView('waiting-room');
  };

  const handleBackToMissions = () => {
    setCurrentView('missions');
    setActiveSession(null);
  };

  return (
    <div className="flex h-screen bg-slate-950 text-slate-50">
      <Sidebar currentView={currentView} onNavigate={setCurrentView} />
      
      <main className="flex-1 overflow-auto">
        {currentView === 'missions' && (
          <MissionsView onMissionCreate={handleMissionSelected} />
        )}
        
        {currentView === 'waiting-room' && activeSession && (
          <WaitingRoomView
            sessionId={activeSession.sessionId}
            missionTitle={activeSession.missionTitle}
            onBack={handleBackToMissions}
          />
        )}
      </main>
    </div>
  );
}
