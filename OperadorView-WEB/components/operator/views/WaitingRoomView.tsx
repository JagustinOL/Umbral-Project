'use client';

import { useState, useEffect } from 'react';
import { ArrowLeft, Copy, Check } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { TeamsList } from '../lists/TeamsList';
import { JoinCodeDisplay } from '../ui/JoinCodeDisplay';
import { StartSessionButton } from '../buttons/StartSessionButton';
import { getMockTeams } from '@/lib/mockData';

interface WaitingRoomViewProps {
  sessionId: string;
  missionTitle: string;
  onBack: () => void;
}

export function WaitingRoomView({
  sessionId,
  missionTitle,
  onBack,
}: WaitingRoomViewProps) {
  const [copiedCode, setCopiedCode] = useState(false);
  const [teams, setTeams] = useState(getMockTeams());
  const [sessionStarting, setSessionStarting] = useState(false);

  const joinCode = 'UMBRAL-' + sessionId.slice(0, 8).toUpperCase();

  const handleCopyCode = () => {
    navigator.clipboard.writeText(joinCode);
    setCopiedCode(true);
    setTimeout(() => setCopiedCode(false), 2000);
  };

  const handleStartSession = () => {
    setSessionStarting(true);
    // Simulate API call
    setTimeout(() => {
      alert('Session started! (This is a simulated action - HU-50)');
      setSessionStarting(false);
    }, 1500);
  };

  // Simulate teams joining over time (demo only)
  useEffect(() => {
    const timer = setTimeout(() => {
      if (teams.length < 4) {
        const newTeam = {
          id: `team-${Date.now()}`,
          name: `Team ${teams.length + 1}`,
          memberCount: Math.floor(Math.random() * 3) + 1,
          joinedAt: new Date().toLocaleTimeString(),
          status: 'approved' as const,
        };
        setTeams([...teams, newTeam]);
      }
    }, 4000);
    return () => clearTimeout(timer);
  }, [teams]);

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8 flex items-center gap-4">
        <Button
          onClick={onBack}
          variant="ghost"
          size="icon"
          className="text-slate-400 hover:text-slate-50"
        >
          <ArrowLeft className="w-5 h-5" />
        </Button>
        <div>
          <h2 className="text-3xl font-bold text-slate-50">{missionTitle}</h2>
          <p className="text-slate-400">Session ID: {sessionId}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-8">
          {/* Join Code Section */}
          <JoinCodeDisplay code={joinCode} onCopy={handleCopyCode} copied={copiedCode} />

          {/* Teams Joined Section - HU-49 */}
          <div>
            <h3 className="text-lg font-semibold text-slate-50 mb-4">
              Teams Joined ({teams.length})
            </h3>
            <TeamsList teams={teams} />
          </div>
        </div>

        {/* Sidebar - Start Button */}
        <div className="lg:col-span-1">
          <div className="sticky top-8 p-6 bg-slate-800 border border-slate-700 rounded-lg">
            <h4 className="font-semibold text-slate-50 mb-4">Session Control</h4>

            {/* Status Info */}
            <div className="mb-6 p-3 bg-slate-900 rounded text-sm text-slate-300 border border-slate-700">
              <p className="font-medium mb-1">Status</p>
              <p className="text-amber-400">Pending</p>
            </div>

            {/* Team Count Info */}
            <div className="mb-6 p-3 bg-slate-900 rounded text-sm">
              <p className="font-medium text-slate-300 mb-1">Teams Ready</p>
              <p className="text-2xl font-bold text-slate-50">{teams.length}</p>
            </div>

            {/* Start Button - HU-50 with RN-15 */}
            <StartSessionButton
              disabled={teams.length === 0}
              loading={sessionStarting}
              onStart={handleStartSession}
            />

            {/* Business Rule Alert - RN-15 */}
            {teams.length === 0 && (
              <div className="mt-4 p-3 bg-red-950 border border-red-800 rounded text-xs text-red-200">
                <p className="font-medium">Cannot start session</p>
                <p className="mt-1">
                  At least 1 team must be approved to start (RN-15)
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
