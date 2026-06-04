"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { LightbulbIcon, Loader2Icon, PencilIcon, PlusIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
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
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Hint, MissionNode } from "@/lib/types";
import {
  getHintApiErrorMessage,
  hintService,
  logHintApiProblem,
  sortHintsByOrder,
  toHintViewModel,
  validateHintAttachment,
  validateHintContent,
} from "@/lib/services/hintService";
import { getStructureLockTooltip } from "@/lib/services/nodeService";
import type { MissionStatus } from "@/lib/types/api";

interface HintPanelProps {
  missionId: string;
  missionStatus: MissionStatus;
  node: MissionNode;
  onHintsChange?: (nodeId: string, hints: Hint[]) => void;
}

const hintsAreEqual = (a: Hint[], b: Hint[]): boolean => {
  if (a.length !== b.length) return false;
  return a.every(
    (hint, index) =>
      hint.id === b[index]?.id &&
      hint.order === b[index]?.order &&
      hint.content === b[index]?.content &&
      hint.penaltyPoints === b[index]?.penaltyPoints,
  );
};

export function HintPanel({ missionId, missionStatus, node, onHintsChange }: HintPanelProps) {
  if (node.type === "Stage") {
    return null;
  }

  const nodeId = node.id;
  const onHintsChangeRef = useRef(onHintsChange);
  onHintsChangeRef.current = onHintsChange;

  const [hints, setHints] = useState<Hint[]>(() => sortHintsByOrder(node.hints ?? []));
  const [isInitialLoading, setIsInitialLoading] = useState(() => (node.hints ?? []).length === 0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [newContent, setNewContent] = useState("");
  const [newAttachment, setNewAttachment] = useState<File | null>(null);
  const [editingHintId, setEditingHintId] = useState<string | null>(null);
  const [editContent, setEditContent] = useState("");
  const [deleteTarget, setDeleteTarget] = useState<Hint | null>(null);

  const isStructureLocked = missionStatus !== "Draft";
  const structureLockTooltip = getStructureLockTooltip(missionStatus);

  const commitHints = useCallback(
    (next: Hint[], notifyParent: boolean) => {
      const sorted = sortHintsByOrder(next);
      setHints((prev) => (hintsAreEqual(prev, sorted) ? prev : sorted));
      if (notifyParent) {
        onHintsChangeRef.current?.(nodeId, sorted);
      }
    },
    [nodeId],
  );

  const loadHints = useCallback(
    async (signal?: AbortSignal, options?: { silent?: boolean }) => {
      if (!options?.silent) {
        setIsInitialLoading(true);
      }

      try {
        const dtos = await hintService.getHintsByNode(missionId, nodeId, signal);
        if (signal?.aborted) return;
        commitHints(dtos.map((dto) => toHintViewModel(dto, nodeId)), true);
      } catch (error) {
        if (signal?.aborted) return;
        logHintApiProblem(error);
        toast.error(getHintApiErrorMessage(error));
      } finally {
        if (!signal?.aborted) {
          setIsInitialLoading(false);
        }
      }
    },
    [commitHints, missionId, nodeId],
  );

  useEffect(() => {
    setIsInitialLoading(true);
    setEditingHintId(null);
    setDeleteTarget(null);

    const controller = new AbortController();
    void loadHints(controller.signal);
    return () => controller.abort();
  }, [missionId, nodeId, loadHints]);

  const handleAddHint = async () => {
    const contentError = validateHintContent(newContent);
    if (contentError) {
      toast.error(contentError);
      return;
    }

    const attachmentError = validateHintAttachment(newAttachment);
    if (attachmentError) {
      toast.error(attachmentError);
      return;
    }

    setIsSubmitting(true);
    try {
      await hintService.addHint(missionId, nodeId, newContent.trim(), newAttachment);
      setNewContent("");
      setNewAttachment(null);
      await loadHints(undefined, { silent: true });
      toast.success("Pista creada (HU-17).");
    } catch (error) {
      logHintApiProblem(error);
      toast.error(getHintApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleStartEdit = (hint: Hint) => {
    setEditingHintId(hint.id);
    setEditContent(hint.content);
  };

  const handleSaveEdit = async (hintId: string) => {
    const contentError = validateHintContent(editContent);
    if (contentError) {
      toast.error(contentError);
      return;
    }

    setIsSubmitting(true);
    try {
      await hintService.updateHint(missionId, nodeId, hintId, {
        content: editContent.trim(),
      });
      setEditingHintId(null);
      setEditContent("");
      await loadHints(undefined, { silent: true });
      toast.success("Pista actualizada (HU-19).");
    } catch (error) {
      logHintApiProblem(error);
      toast.error(getHintApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteHint = async () => {
    if (!deleteTarget) return;

    setIsSubmitting(true);
    try {
      await hintService.deleteHint(missionId, nodeId, deleteTarget.id);
      setDeleteTarget(null);
      await loadHints(undefined, { silent: true });
      toast.success("Pista eliminada (HU-20).");
    } catch (error) {
      logHintApiProblem(error);
      toast.error(getHintApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mt-3 space-y-2">
      <p className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
        <LightbulbIcon className="h-3.5 w-3.5" />
        Pistas ({hints.length}) — orden de liberación (HU-21)
        {isSubmitting && <Loader2Icon className="h-3 w-3 animate-spin" />}
      </p>

      {isInitialLoading ? (
        <div className="space-y-1.5">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      ) : (
        <div className="space-y-1.5">
          {hints.length === 0 && (
            <p className="text-xs text-muted-foreground italic py-1">Sin pistas configuradas (HU-18).</p>
          )}
          {hints.map((hint) => (
            <div
              key={hint.id}
              className="rounded-md border border-border bg-muted/30 px-3 py-2 text-xs space-y-2"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-medium text-muted-foreground shrink-0">#{hint.order}</span>
                <span className="text-muted-foreground shrink-0">−{hint.penaltyPoints} pts</span>
              </div>
              {editingHintId === hint.id ? (
                <div className="space-y-2">
                  <Textarea
                    value={editContent}
                    onChange={(e) => setEditContent(e.target.value)}
                    rows={2}
                    className="text-xs resize-none"
                    disabled={isSubmitting}
                  />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="secondary"
                      className="h-7 text-xs"
                      disabled={isSubmitting}
                      onClick={() => void handleSaveEdit(hint.id)}
                    >
                      {isSubmitting ? "Guardando…" : "Guardar (HU-19)"}
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-7 text-xs"
                      disabled={isSubmitting}
                      onClick={() => {
                        setEditingHintId(null);
                        setEditContent("");
                      }}
                    >
                      Cancelar
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex items-start gap-2">
                  <span className="flex-1 text-foreground leading-relaxed">{hint.content}</span>
                  {!isStructureLocked && (
                    <div className="flex shrink-0 gap-0.5">
                      <button
                        type="button"
                        onClick={() => handleStartEdit(hint)}
                        className="text-muted-foreground hover:text-foreground transition-colors p-0.5"
                        aria-label="Editar pista"
                        disabled={isSubmitting}
                      >
                        <PencilIcon className="h-3 w-3" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteTarget(hint)}
                        className="text-muted-foreground hover:text-destructive transition-colors p-0.5"
                        aria-label="Eliminar pista"
                        disabled={isSubmitting}
                      >
                        <Trash2Icon className="h-3 w-3" />
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {!isStructureLocked && (
        <div className="space-y-2 pt-1">
          <Textarea
            value={newContent}
            onChange={(e) => setNewContent(e.target.value)}
            placeholder="Contenido de la nueva pista…"
            rows={2}
            className="text-xs resize-none"
            disabled={isSubmitting}
          />
          <div className="space-y-1">
            <Label htmlFor={`hint-file-${nodeId}`} className="text-xs text-muted-foreground">
              Adjunto opcional (JPG/PNG, máx. 5 MB)
            </Label>
            <Input
              id={`hint-file-${nodeId}`}
              type="file"
              accept="image/jpeg,image/png,.jpg,.jpeg,.png"
              className="text-xs h-8"
              disabled={isSubmitting}
              onChange={(e) => setNewAttachment(e.target.files?.[0] ?? null)}
            />
            {newAttachment && (
              <p className="text-xs text-muted-foreground truncate">{newAttachment.name}</p>
            )}
          </div>
          <Button
            size="sm"
            variant="outline"
            className="h-8 gap-1 text-xs"
            disabled={isSubmitting}
            onClick={() => void handleAddHint()}
          >
            {isSubmitting ? (
              <Loader2Icon className="h-3 w-3 animate-spin" />
            ) : (
              <PlusIcon className="h-3 w-3" />
            )}
            Añadir pista (HU-17)
          </Button>
        </div>
      )}

      {isStructureLocked && (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <p className="text-xs text-muted-foreground italic cursor-help">{structureLockTooltip}</p>
            </TooltipTrigger>
            <TooltipContent side="top" className="text-xs max-w-[220px]">
              {structureLockTooltip}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )}

      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Eliminar pista</AlertDialogTitle>
            <AlertDialogDescription>
              Se eliminará la pista #{deleteTarget?.order}. Esta acción no se puede deshacer (HU-20).
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isSubmitting}>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={isSubmitting}
              onClick={() => void handleDeleteHint()}
            >
              {isSubmitting ? "Eliminando…" : "Eliminar"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
