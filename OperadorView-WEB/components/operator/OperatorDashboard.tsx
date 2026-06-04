"use client";

import { useCallback, useEffect, useState } from "react";
import { Toaster, toast } from "sonner";
import { Sidebar } from "./Sidebar";
import { MissionsView } from "./views/MissionsView";
import { WaitingRoomView } from "./views/WaitingRoomView";
import { getOperatorId } from "@/lib/config";
import {
  getOperatorSessionApiErrorMessage,
  operatorSessionService,
} from "@/lib/services/operatorSessionService";
import {
  getOperatorProfileErrorMessage,
  operatorService,
} from "@/lib/services/operatorService";
import { OperatorAssignedMissionDto } from "@/lib/types/api";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertTriangleIcon } from "lucide-react";

export type ViewType = "missions" | "waiting-room";

interface ActiveSession {
  sessionId: string;
  missionId: string;
  missionTitle: string;
  joinCode: string;
}

interface OperatorProfile {
  operatorId: string;
  displayName: string;
  email: string;
}

export function OperatorDashboard() {
  const [operatorId, setOperatorId] = useState<string | null>(() => getOperatorId());
  const [currentView, setCurrentView] = useState<ViewType>("missions");
  const [activeSession, setActiveSession] = useState<ActiveSession | null>(null);
  const [missions, setMissions] = useState<OperatorAssignedMissionDto[]>([]);
  const [openSessionByMission, setOpenSessionByMission] = useState<Record<string, boolean>>({});
  const [operatorProfile, setOperatorProfile] = useState<OperatorProfile | null>(null);
  const [isLoadingMissions, setIsLoadingMissions] = useState(true);
  const [isCreatingSession, setIsCreatingSession] = useState(false);
  const [creatingMissionId, setCreatingMissionId] = useState<string | null>(null);
  const [missionsError, setMissionsError] = useState<string | null>(null);
  const [configError, setConfigError] = useState<string | null>(null);

  const loadMissions = useCallback(
    async (signal?: AbortSignal) => {
      if (!operatorId) {
        setConfigError(
          "Sign in at http://localhost:3002 or configure NEXT_PUBLIC_OPERATOR_ID.",
        );
        setMissions([]);
        setIsLoadingMissions(false);
        return;
      }

      setIsLoadingMissions(true);
      setMissionsError(null);
      setConfigError(null);

      try {
        const assigned = await operatorSessionService.getAssignedMissions(operatorId, signal);
        setMissions(assigned);

        const openFlags = await Promise.all(
          assigned.map(async (mission) => {
            try {
              const hasOpen = await operatorSessionService.missionHasOpenSessions(
                mission.missionId,
                signal,
              );
              return [mission.missionId, hasOpen] as const;
            } catch {
              return [mission.missionId, false] as const;
            }
          }),
        );
        setOpenSessionByMission(Object.fromEntries(openFlags));
      } catch (error) {
        if (signal?.aborted) return;
        setMissions([]);
        setMissionsError(getOperatorSessionApiErrorMessage(error));
      } finally {
        if (!signal?.aborted) {
          setIsLoadingMissions(false);
        }
      }
    },
    [operatorId],
  );

  useEffect(() => {
    const controller = new AbortController();
    void loadMissions(controller.signal);
    return () => controller.abort();
  }, [loadMissions]);

  useEffect(() => {
    if (!operatorId) return;

    const controller = new AbortController();
    void (async () => {
      try {
        const profile = await operatorService.getOperatorById(operatorId, controller.signal);
        if (profile) {
          setOperatorProfile({
            operatorId: profile.operatorId,
            displayName: `${profile.firstName} ${profile.lastName}`.trim(),
            email: profile.email,
          });
        } else {
          setOperatorProfile({
            operatorId,
            displayName: "Operador",
            email: "",
          });
        }
      } catch (error) {
        if (controller.signal.aborted) return;
        console.warn(getOperatorProfileErrorMessage(error));
        setOperatorProfile({
          operatorId,
          displayName: "Operador",
          email: "",
        });
      }
    })();

    return () => controller.abort();
  }, [operatorId]);

  const handleCreateSession = async (missionId: string, missionTitle: string) => {
    if (!operatorId) return;

    if (openSessionByMission[missionId]) {
      toast.error("Esta misión ya tiene una sesión abierta. Finalícela antes de crear otra.");
      return;
    }

    setIsCreatingSession(true);
    setCreatingMissionId(missionId);

    try {
      const created = await operatorSessionService.createSession(operatorId, missionId);
      setActiveSession({
        sessionId: created.sessionId,
        missionId,
        missionTitle,
        joinCode: created.joinCode,
      });
      setCurrentView("waiting-room");
      toast.success("Sesión creada. Comparte el código con los equipos.");
    } catch (error) {
      toast.error(getOperatorSessionApiErrorMessage(error));
    } finally {
      setIsCreatingSession(false);
      setCreatingMissionId(null);
    }
  };

  const handleStartSession = async () => {
    if (!operatorId || !activeSession) return;

    await operatorSessionService.startSession(operatorId, activeSession.sessionId);
    toast.success("Sesión iniciada correctamente.");
    setActiveSession(null);
    setCurrentView("missions");
    await loadMissions();
  };

  const handleBackToMissions = () => {
    setCurrentView("missions");
    setActiveSession(null);
    void loadMissions();
  };

  return (
    <div className="flex h-screen overflow-hidden bg-background font-sans">
      <Toaster richColors closeButton position="top-right" />
      <Sidebar
        currentView={currentView}
        onNavigate={setCurrentView}
        operatorProfile={operatorProfile}
      />

      <main className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-6 py-8">
          {configError && (
            <Alert variant="destructive" className="mb-6">
              <AlertTriangleIcon className="h-4 w-4" />
              <AlertTitle>Configuración requerida</AlertTitle>
              <AlertDescription>{configError}</AlertDescription>
            </Alert>
          )}

          {currentView === "missions" && (
            <MissionsView
              missions={missions}
              openSessionByMission={openSessionByMission}
              isLoading={isLoadingMissions}
              isCreating={isCreatingSession}
              creatingMissionId={creatingMissionId}
              errorMessage={missionsError}
              onRetry={() => void loadMissions()}
              onCreateSession={handleCreateSession}
            />
          )}

          {currentView === "waiting-room" && activeSession && operatorId && (
            <WaitingRoomView
              operatorId={operatorId}
              sessionId={activeSession.sessionId}
              missionTitle={activeSession.missionTitle}
              joinCode={activeSession.joinCode}
              onBack={handleBackToMissions}
              onStartSession={handleStartSession}
            />
          )}
        </div>
      </main>
    </div>
  );
}
