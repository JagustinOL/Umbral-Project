"use client";

import { useCallback, useEffect, useState } from "react";
import { Sidebar, NavSection } from "@/components/umbral/Sidebar";
import { MissionCatalog } from "@/components/umbral/MissionCatalog";
import { MissionBuilder } from "@/components/umbral/MissionBuilder";
import { OperatorManagement } from "@/components/umbral/OperatorManagement";
import { AuditHistory } from "@/components/umbral/AuditHistory";
import {
  CreateMissionPayload,
  CreateOperatorPayload,
  Mission,
  Operator,
  UpdateMissionPayload,
} from "@/lib/types";
import {
  getMissionApiErrorMessage,
  getMissionOperatorAssignmentErrorMessage,
  logMissionOperatorApiProblem,
  missionService,
  toMissionViewModel,
} from "@/lib/services/missionService";
import {
  getOperatorApiErrorMessage,
  operatorService,
  toOperatorViewModel,
} from "@/lib/services/operatorService";
import { toast, Toaster } from "sonner";

export default function DashboardPage() {
  const [section, setSection] = useState<NavSection>("catalog");
  const [missions, setMissions] = useState<Mission[]>([]);
  const [operators, setOperators] = useState<Operator[]>([]);
  const [activeMission, setActiveMission] = useState<Mission | null>(null);
  const [isLoadingMissions, setIsLoadingMissions] = useState(true);
  const [isCreatingMission, setIsCreatingMission] = useState(false);
  const [isUpdatingMission, setIsUpdatingMission] = useState(false);
  const [isDeletingMission, setIsDeletingMission] = useState(false);
  const [isLoadingOperators, setIsLoadingOperators] = useState(true);
  const [isCreatingOperator, setIsCreatingOperator] = useState(false);
  const [isDeactivatingOperator, setIsDeactivatingOperator] = useState(false);
  const [missionsError, setMissionsError] = useState<string | null>(null);
  const [operatorsError, setOperatorsError] = useState<string | null>(null);
  const [assignmentError, setAssignmentError] = useState<string | null>(null);
  const [isAssigningOperator, setIsAssigningOperator] = useState(false);
  const [isRevokingOperator, setIsRevokingOperator] = useState(false);

  const loadMissions = useCallback(async (signal?: AbortSignal) => {
    setIsLoadingMissions(true);
    setMissionsError(null);
    try {
      const apiMissions = await missionService.getMissions(signal);
      const mapped = apiMissions.map(toMissionViewModel);
      setMissions(mapped);
      setActiveMission((prev) => (prev ? mapped.find((m) => m.id === prev.id) ?? null : null));
    } catch (error) {
      if (signal?.aborted) return;
      setMissions([]);
      setActiveMission(null);
      setMissionsError(getMissionApiErrorMessage(error));
    } finally {
      if (!signal?.aborted) {
        setIsLoadingMissions(false);
      }
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void loadMissions(controller.signal);
    return () => controller.abort();
  }, [loadMissions]);

  const loadOperators = useCallback(async (signal?: AbortSignal) => {
    setIsLoadingOperators(true);
    setOperatorsError(null);
    try {
      const apiOperators = await operatorService.getOperators(signal);
      setOperators(
        apiOperators.map(toOperatorViewModel).filter((o) => o.status === "Active"),
      );
    } catch (error) {
      if (signal?.aborted) return;
      setOperators([]);
      setOperatorsError(getOperatorApiErrorMessage(error));
    } finally {
      if (!signal?.aborted) {
        setIsLoadingOperators(false);
      }
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void loadOperators(controller.signal);
    return () => controller.abort();
  }, [loadOperators]);

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

  const handleCreateMission = async (payload: CreateMissionPayload) => {
    setIsCreatingMission(true);
    try {
      await missionService.createMission(payload);
      await loadMissions();
    } catch (error) {
      setMissionsError(getMissionApiErrorMessage(error));
      throw error;
    } finally {
      setIsCreatingMission(false);
    }
  };

  const handleUpdateMission = async (missionId: string, payload: UpdateMissionPayload) => {
    setIsUpdatingMission(true);
    setMissionsError(null);
    try {
      await missionService.updateMission(missionId, payload);
      await loadMissions();
    } catch (error) {
      setMissionsError(getMissionApiErrorMessage(error));
      throw error;
    } finally {
      setIsUpdatingMission(false);
    }
  };

  const handleDeleteMission = async (missionId: string) => {
    setIsDeletingMission(true);
    setMissionsError(null);
    try {
      let hasOpenSessions = false;
      try {
        hasOpenSessions = await missionService.missionHasOpenSessions(missionId);
      } catch (precheckError) {
        // If precheck is temporarily unavailable, fallback to DELETE and let API enforce RN-01.
        console.warn("Mission precheck failed, fallback to DELETE flow:", precheckError);
      }

      if (hasOpenSessions) {
        setMissionsError(
          "Mission deactivation blocked by RN-01: there are active or open sessions linked to this mission.",
        );
        throw new Error("Mission has open sessions.");
      }

      await missionService.deleteMission(missionId);
      await loadMissions();
    } catch (error) {
      if (!(error instanceof Error && error.message === "Mission has open sessions.")) {
        setMissionsError(getMissionApiErrorMessage(error));
      }
      throw error;
    } finally {
      setIsDeletingMission(false);
    }
  };

  const handleCreateOperator = async (payload: CreateOperatorPayload) => {
    setIsCreatingOperator(true);
    setOperatorsError(null);
    try {
      const result = await operatorService.createOperator(payload);
      await loadOperators();
      return result;
    } catch (error) {
      setOperatorsError(getOperatorApiErrorMessage(error));
      throw error;
    } finally {
      setIsCreatingOperator(false);
    }
  };

  const handleResendOperatorActivation = async (operatorId: string) => {
    setOperatorsError(null);
    try {
      return await operatorService.resendActivation(operatorId);
    } catch (error) {
      setOperatorsError(getOperatorApiErrorMessage(error));
      throw error;
    }
  };

  const handleDeactivateOperator = async (operatorId: string) => {
    setIsDeactivatingOperator(true);
    setOperatorsError(null);
    try {
      await operatorService.deactivateOperator(operatorId);
      await loadOperators();
    } catch (error) {
      setOperatorsError(getOperatorApiErrorMessage(error));
      throw error;
    } finally {
      setIsDeactivatingOperator(false);
    }
  };

  const handleRetryLoadMissions = () => {
    void loadMissions();
  };

  const handleRetryLoadOperators = () => {
    void loadOperators();
  };

  const ensureMissionAllowsOperatorRosterChange = async (missionId: string): Promise<void> => {
    try {
      const hasOpenSessions = await missionService.missionHasOpenSessions(missionId);
      if (hasOpenSessions) {
        const message =
          "RN-01: No se puede modificar la asignación de operadores mientras la misión tiene sesiones abiertas.";
        setAssignmentError(message);
        throw new Error(message);
      }
    } catch (error) {
      if (error instanceof Error && error.message.includes("RN-01")) {
        throw error;
      }
      console.warn("Mission session precheck unavailable, continuing with API enforcement:", error);
    }
  };

  const handleAssignOperator = async (missionId: string, operatorId: string) => {
    setIsAssigningOperator(true);
    setAssignmentError(null);
    try {
      await ensureMissionAllowsOperatorRosterChange(missionId);
      await missionService.assignOperatorToMission(missionId, operatorId);
      await loadMissions();
      toast.success("Operador asignado a la misión (HU-24).");
    } catch (error) {
      if (!(error instanceof Error && error.message.includes("RN-01"))) {
        logMissionOperatorApiProblem(error);
        const message = getMissionOperatorAssignmentErrorMessage(error);
        setAssignmentError(message);
        toast.error(message);
      } else {
        toast.error(error instanceof Error ? error.message : "RN-01");
      }
      throw error;
    } finally {
      setIsAssigningOperator(false);
    }
  };

  const handleRevokeOperator = async (missionId: string, operatorId: string) => {
    setIsRevokingOperator(true);
    setAssignmentError(null);
    try {
      await ensureMissionAllowsOperatorRosterChange(missionId);
      await missionService.revokeOperatorFromMission(missionId, operatorId);
      await loadMissions();
      toast.success("Operador revocado de la misión (HU-25).");
    } catch (error) {
      if (!(error instanceof Error && error.message.includes("RN-01"))) {
        logMissionOperatorApiProblem(error);
        const message = getMissionOperatorAssignmentErrorMessage(error);
        setAssignmentError(message);
        toast.error(message);
      } else {
        toast.error(error instanceof Error ? error.message : "RN-01");
      }
      throw error;
    } finally {
      setIsRevokingOperator(false);
    }
  };

  return (
    <div className="flex h-screen overflow-hidden bg-background font-sans">
      <Toaster richColors closeButton position="top-right" />
      <Sidebar active={section} onNavigate={handleNavigate} />

      <main className="flex-1 min-h-0 min-w-0 overflow-y-auto overscroll-contain">
        <div className="max-w-5xl mx-auto w-full px-6 py-8">
          {section === "catalog" && !activeMission && (
            <MissionCatalog
              missions={missions}
              onOpenBuilder={handleOpenBuilder}
              onCreateMission={handleCreateMission}
              onUpdateMission={handleUpdateMission}
              onDeleteMission={handleDeleteMission}
              isLoading={isLoadingMissions}
              isCreating={isCreatingMission}
              isUpdating={isUpdatingMission}
              isDeleting={isDeletingMission}
              errorMessage={missionsError}
              onRetry={handleRetryLoadMissions}
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
              onCreateOperator={handleCreateOperator}
              onResendActivation={handleResendOperatorActivation}
              onDeactivateOperator={handleDeactivateOperator}
              onAssignOperator={handleAssignOperator}
              onRevokeOperator={handleRevokeOperator}
              isLoading={isLoadingOperators}
              isCreating={isCreatingOperator}
              isDeactivating={isDeactivatingOperator}
              isAssigning={isAssigningOperator}
              isRevoking={isRevokingOperator}
              errorMessage={operatorsError}
              assignmentError={assignmentError}
              onRetry={handleRetryLoadOperators}
              onClearAssignmentError={() => setAssignmentError(null)}
            />
          )}

          {section === "audit" && <AuditHistory />}
        </div>
      </main>
    </div>
  );
}
