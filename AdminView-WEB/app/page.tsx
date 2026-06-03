"use client";

import { useState } from "react";
import { Sidebar, NavSection } from "@/components/umbral/Sidebar";
import { MissionCatalog } from "@/components/umbral/MissionCatalog";
import { MissionBuilder } from "@/components/umbral/MissionBuilder";
import { OperatorManagement } from "@/components/umbral/OperatorManagement";
import { Mission, Operator } from "@/lib/types";
import { MOCK_MISSIONS, MOCK_OPERATORS } from "@/lib/mock-data";

export default function DashboardPage() {
  const [section, setSection] = useState<NavSection>("catalog");
  const [missions, setMissions] = useState<Mission[]>(MOCK_MISSIONS);
  const [operators, setOperators] = useState<Operator[]>(MOCK_OPERATORS);
  const [activeMission, setActiveMission] = useState<Mission | null>(null);

  const handleNavigate = (next: NavSection) => {
    setSection(next);
    setActiveMission(null);
  };

  const handleOpenBuilder = (mission: Mission) => {
    setActiveMission(mission);
  };

  const handleBuilderBack = () => {
    setActiveMission(null);
  };

  const handleMissionChange = (updated: Mission) => {
    setMissions((prev) => prev.map((m) => (m.id === updated.id ? updated : m)));
    setActiveMission(updated);
  };

  const handleMissionsChange = (updated: Mission[]) => {
    setMissions(updated);
    if (activeMission) {
      const found = updated.find((m) => m.id === activeMission.id);
      setActiveMission(found ?? null);
    }
  };

  return (
    <div className="flex h-screen overflow-hidden bg-background font-sans">
      <Sidebar active={section} onNavigate={handleNavigate} />

      <main className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-6 py-8">
          {section === "catalog" && !activeMission && (
            <MissionCatalog
              missions={missions}
              onMissionsChange={handleMissionsChange}
              onOpenBuilder={handleOpenBuilder}
            />
          )}

          {section === "catalog" && activeMission && (
            <MissionBuilder
              mission={activeMission}
              onBack={handleBuilderBack}
              onMissionChange={handleMissionChange}
            />
          )}

          {section === "operators" && (
            <OperatorManagement
              operators={operators}
              missions={missions}
              onOperatorsChange={setOperators}
              onMissionsChange={setMissions}
            />
          )}
        </div>
      </main>
    </div>
  );
}
