"use client";

import { useState } from "react";
import {
  PlusIcon,
  PencilIcon,
  Trash2Icon,
  ClockIcon,
  ChevronRightIcon,
  AlertTriangleIcon,
  ArchiveIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "./StatusBadge";
import { DifficultyStars } from "./DifficultyStars";
import { MissionFormModal } from "./MissionFormModal";
import { Mission, CreateMissionPayload, UpdateMissionPayload } from "@/lib/types";
import { cn } from "@/lib/utils";

interface MissionCatalogProps {
  missions: Mission[];
  onOpenBuilder: (mission: Mission) => void;
  onCreateMission: (payload: CreateMissionPayload) => Promise<void>;
  onUpdateMission: (missionId: string, payload: UpdateMissionPayload) => Promise<void>;
  onDeleteMission: (missionId: string) => Promise<void>;
  isLoading: boolean;
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  errorMessage: string | null;
  onRetry: () => void;
}

export function MissionCatalog({
  missions,
  onOpenBuilder,
  onCreateMission,
  onUpdateMission,
  onDeleteMission,
  isLoading,
  isCreating,
  isUpdating,
  isDeleting,
  errorMessage,
  onRetry,
}: MissionCatalogProps) {
  const [createOpen, setCreateOpen] = useState(false);
  const [editMission, setEditMission] = useState<Mission | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Mission | null>(null);

  const activeMissions = missions.filter((m) => m.status !== "Inactive");
  const inactiveMissions = missions.filter((m) => m.status === "Inactive");

  const handleCreate = async (data: CreateMissionPayload | UpdateMissionPayload) => {
    const payload = data as CreateMissionPayload;
    try {
      await onCreateMission(payload);
      setCreateOpen(false);
    } catch {
      // Error is handled at page-level and rendered as alert.
    }
  };

  const handleEdit = async (data: CreateMissionPayload | UpdateMissionPayload) => {
    if (!editMission) return;
    const payload = data as UpdateMissionPayload;
    try {
      await onUpdateMission(editMission.id, payload);
      setEditMission(null);
    } catch {
      // Error is handled at page-level and rendered as alert.
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await onDeleteMission(deleteTarget.id);
      setDeleteTarget(null);
    } catch {
      // Error is handled at page-level and rendered as alert.
    }
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground text-balance">Mission Catalog</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {activeMissions.length} mission{activeMissions.length !== 1 ? "s" : ""} in use
            {inactiveMissions.length > 0 &&
              ` · ${inactiveMissions.length} deactivated for audit`}
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} size="sm" className="gap-1.5">
          <PlusIcon className="h-4 w-4" />
          New Mission
        </Button>
      </div>

      {/* Table */}
      <div className="rounded-lg border border-border bg-card overflow-hidden">
        {errorMessage && (
          <Alert variant="destructive" className="m-3 mb-0">
            <AlertTriangleIcon className="h-4 w-4" />
            <AlertTitle>Mission API error</AlertTitle>
            <AlertDescription className="flex items-center justify-between gap-2">
              <span>{errorMessage}</span>
              <Button variant="outline" size="sm" onClick={onRetry}>
                Retry
              </Button>
            </AlertDescription>
          </Alert>
        )}
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/40 hover:bg-muted/40">
              <TableHead className="font-medium text-foreground w-[280px]">Title</TableHead>
              <TableHead className="font-medium text-foreground">Status</TableHead>
              <TableHead className="font-medium text-foreground">Difficulty</TableHead>
              <TableHead className="font-medium text-foreground">Duration</TableHead>
              <TableHead className="font-medium text-foreground">Operators</TableHead>
              <TableHead className="font-medium text-foreground text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, index) => (
                <TableRow key={`skeleton-${index}`}>
                  <TableCell>
                    <Skeleton className="h-4 w-36" />
                    <Skeleton className="h-3 w-56 mt-2" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-6 w-16" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-16" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-14" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-8" />
                  </TableCell>
                  <TableCell>
                    <div className="flex justify-end gap-2">
                      <Skeleton className="h-7 w-7" />
                      <Skeleton className="h-7 w-7" />
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : errorMessage ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-12 text-muted-foreground text-sm">
                  Unable to load missions. Please retry when the Mission API is available.
                </TableCell>
              </TableRow>
            ) : activeMissions.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-12 text-muted-foreground text-sm">
                  No missions yet. Create your first mission to get started.
                </TableCell>
              </TableRow>
            ) : (
              activeMissions.map((mission) => {
                const isImmutable = mission.status === "Active";
                return (
                  <TableRow
                    key={mission.id}
                    className="group cursor-pointer hover:bg-muted/30 transition-colors"
                    onClick={() => onOpenBuilder(mission)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div>
                          <p className="font-medium text-sm text-foreground">{mission.title}</p>
                          <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1 max-w-[240px]">
                            {mission.description}
                          </p>
                        </div>
                        <ChevronRightIcon className="h-4 w-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity ml-auto shrink-0" />
                      </div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={mission.status} />
                    </TableCell>
                    <TableCell>
                      <DifficultyStars value={mission.difficulty} />
                    </TableCell>
                    <TableCell>
                      {mission.maxDurationMinutes ? (
                        <span className="flex items-center gap-1 text-sm text-muted-foreground">
                          <ClockIcon className="h-3.5 w-3.5" />
                          {mission.maxDurationMinutes} min
                        </span>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {mission.assignedOperators?.length ?? 0}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div
                        className="flex items-center justify-end gap-1"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <TooltipProvider>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <span>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  className="h-7 w-7"
                                  disabled={isImmutable || isUpdating || isDeleting}
                                  onClick={() => setEditMission(mission)}
                                >
                                  <PencilIcon className="h-3.5 w-3.5" />
                                  <span className="sr-only">Edit</span>
                                </Button>
                              </span>
                            </TooltipTrigger>
                            {isImmutable && (
                              <TooltipContent side="top" className="text-xs max-w-[180px]">
                                RN-01: Active missions are immutable. Deactivate first to edit.
                              </TooltipContent>
                            )}
                          </Tooltip>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <span>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  className={cn("h-7 w-7", !isImmutable && "text-destructive hover:text-destructive")}
                                  disabled={isImmutable || isUpdating || isDeleting}
                                  onClick={() => setDeleteTarget(mission)}
                                >
                                  <Trash2Icon className="h-3.5 w-3.5" />
                                  <span className="sr-only">Delete</span>
                                </Button>
                              </span>
                            </TooltipTrigger>
                            {isImmutable && (
                              <TooltipContent side="top" className="text-xs max-w-[180px]">
                                RN-01: Active missions cannot be deleted.
                              </TooltipContent>
                            )}
                          </Tooltip>
                        </TooltipProvider>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {inactiveMissions.length > 0 && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-2">
            <ArchiveIcon className="h-4 w-4 text-muted-foreground" />
            <div>
              <h2 className="text-sm font-medium text-foreground">Deactivated missions</h2>
              <p className="text-xs text-muted-foreground">
                Read-only audit view. Inactive missions cannot be edited or reactivated.
              </p>
            </div>
          </div>
          <div className="rounded-lg border border-border bg-card overflow-hidden opacity-90">
            <Table>
              <TableHeader>
                <TableRow className="bg-muted/40 hover:bg-muted/40">
                  <TableHead className="font-medium text-foreground w-[280px]">Title</TableHead>
                  <TableHead className="font-medium text-foreground">Status</TableHead>
                  <TableHead className="font-medium text-foreground">Difficulty</TableHead>
                  <TableHead className="font-medium text-foreground">Duration</TableHead>
                  <TableHead className="font-medium text-foreground">Operators</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {inactiveMissions.map((mission) => (
                  <TableRow key={mission.id} className="hover:bg-muted/20">
                    <TableCell>
                      <div>
                        <p className="font-medium text-sm text-foreground">{mission.title}</p>
                        <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1 max-w-[240px]">
                          {mission.description}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={mission.status} />
                    </TableCell>
                    <TableCell>
                      <DifficultyStars value={mission.difficulty} />
                    </TableCell>
                    <TableCell>
                      {mission.maxDurationMinutes ? (
                        <span className="flex items-center gap-1 text-sm text-muted-foreground">
                          <ClockIcon className="h-3.5 w-3.5" />
                          {mission.maxDurationMinutes} min
                        </span>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {mission.assignedOperators?.length ?? 0}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      )}

      {/* Create Modal */}
      <MissionFormModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreate}
        isSubmitting={isCreating}
      />

      {/* Edit Modal */}
      <MissionFormModal
        open={!!editMission}
        onClose={() => setEditMission(null)}
        onSubmit={handleEdit}
        mission={editMission}
        isSubmitting={isUpdating}
      />

      {/* Delete Confirmation */}
      <AlertDialog open={!!deleteTarget} onOpenChange={(v) => !v && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate Mission</AlertDialogTitle>
            <AlertDialogDescription>
              This will deactivate &ldquo;{deleteTarget?.title}&rdquo; and mark it as Inactive. This action
              satisfies <strong>RN-01</strong>.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => void handleDelete()}
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={isDeleting}
            >
              {isDeleting ? "Deactivating..." : "Deactivate"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
