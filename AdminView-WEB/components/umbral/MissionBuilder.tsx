"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  ChevronLeftIcon,
  PlusIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  MapPinIcon,
  BrainCircuitIcon,
  Layers3Icon,
  Trash2Icon,
  AlertTriangleIcon,
  Loader2Icon,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
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
import { StatusBadge } from "./StatusBadge";
import { DifficultyStars } from "./DifficultyStars";
import { HintPanel } from "./HintPanel";
import { TriviaNodeForm } from "./TriviaNodeForm";
import { TreasureHuntForm } from "./TreasureHuntForm";
import { Mission, MissionNode, Hint, MissionNodeType, TriviaQuestion } from "@/lib/types";
import {
  getNodeApiErrorMessage,
  getStructureLockTooltip,
  logNodeApiProblem,
  nodeService,
  sortStagesByExecutionOrder,
  toStageViewModel,
} from "@/lib/services/nodeService";
import {
  gameService,
  getGameApiErrorMessage,
  loadStageGames,
  logGameApiProblem,
  toApiTriviaQuestions,
  toTreasureHuntGameViewModel,
  toTriviaGameViewModel,
  validateBaseScore,
  validateTreasureHuntPayload,
  validateTriviaQuestions,
} from "@/lib/services/gameService";
import {
  AddGameTypeDialog,
  CreateTreasureHuntDialog,
  CreateTriviaDialog,
} from "./GameCreateDialogs";
import { cn } from "@/lib/utils";

interface MissionBuilderProps {
  mission: Mission;
  onBack: () => void;
  onMissionChange: (updated: Mission) => void;
}

function LockedAction({
  locked,
  tooltip,
  children,
}: {
  locked: boolean;
  tooltip: string;
  children: React.ReactElement;
}) {
  if (!locked) return children;

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="inline-flex">{children}</span>
        </TooltipTrigger>
        <TooltipContent side="top" className="text-xs max-w-[220px]">
          {tooltip}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

// ─── Add Stage Dialog ──────────────────────────────────────────────────────────

interface AddStageDialogProps {
  open: boolean;
  onClose: () => void;
  onAdd: (title: string, description: string, executionOrder: number) => Promise<void>;
  nextOrder: number;
  isSubmitting: boolean;
}

function AddStageDialog({ open, onClose, onAdd, nextOrder, isSubmitting }: AddStageDialogProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [order, setOrder] = useState(nextOrder);

  useEffect(() => {
    if (open) setOrder(nextOrder);
  }, [open, nextOrder]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onAdd(title, description, order);
    setTitle("");
    setDescription("");
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Añadir etapa</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Define una nueva etapa y su orden de ejecución.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 pt-2">
          <div className="space-y-1.5">
            <Label htmlFor="stage-title">
              Título <span className="text-destructive">*</span>
            </Label>
            <Input
              id="stage-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Nombre de la etapa…"
              required
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="stage-desc">Descripción</Label>
            <Textarea
              id="stage-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={2}
              placeholder="Descripción de la etapa…"
              className="resize-none"
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="stage-order">Orden de ejecución</Label>
            <Input
              id="stage-order"
              type="number"
              min={1}
              value={order}
              onChange={(e) => setOrder(parseInt(e.target.value, 10) || 1)}
              disabled={isSubmitting}
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Guardando…" : "Añadir etapa"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Game Node Card ────────────────────────────────────────────────────────────

interface GameNodeCardProps {
  missionId: string;
  missionStatus: Mission["status"];
  node: MissionNode;
  isStructureLocked: boolean;
  isSaving: boolean;
  isDeleting: boolean;
  onNodeChange: (nodeId: string, patch: Partial<MissionNode>) => void;
  onHintsChange: (nodeId: string, hints: Hint[]) => void;
  onSave: (nodeId: string) => Promise<void>;
  onDelete: (nodeId: string) => void;
}

function GameNodeCard({
  missionId,
  missionStatus,
  node,
  isStructureLocked,
  isSaving,
  isDeleting,
  onNodeChange,
  onHintsChange,
  onSave,
  onDelete,
}: GameNodeCardProps) {
  const [expanded, setExpanded] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const isTrivia = node.type === "Trivia";
  const isTreasure = node.type === "TreasureHunt";

  return (
    <div
      className={cn(
        "rounded-lg border bg-card",
        isTrivia ? "border-blue-200" : "border-amber-200",
      )}
    >
      <Collapsible open={expanded} onOpenChange={setExpanded}>
        <CollapsibleTrigger asChild>
          <div
            className={cn(
              "flex items-center gap-2 px-3 py-2.5 cursor-pointer select-none rounded-lg",
              expanded && "rounded-b-none",
              isTrivia ? "bg-blue-50/50" : "bg-amber-50/50",
            )}
          >
            {expanded ? (
              <ChevronDownIcon className="h-4 w-4 text-muted-foreground shrink-0" />
            ) : (
              <ChevronRightIcon className="h-4 w-4 text-muted-foreground shrink-0" />
            )}
            {isTrivia ? (
              <BrainCircuitIcon className="h-4 w-4 text-blue-600 shrink-0" />
            ) : (
              <MapPinIcon className="h-4 w-4 text-amber-600 shrink-0" />
            )}
            <span className="text-sm font-medium text-foreground flex-1">{node.title}</span>
            <span
              className={cn(
                "text-xs px-2 py-0.5 rounded-full font-medium shrink-0",
                isTrivia ? "bg-blue-100 text-blue-700" : "bg-amber-100 text-amber-700",
              )}
            >
              {isTrivia ? "Trivia" : "Treasure Hunt"}
            </span>
            {!isStructureLocked && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setDeleteOpen(true);
                }}
                className="text-muted-foreground hover:text-destructive transition-colors shrink-0"
                aria-label="Eliminar juego"
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <Loader2Icon className="h-3.5 w-3.5 animate-spin" />
                ) : (
                  <Trash2Icon className="h-3.5 w-3.5" />
                )}
              </button>
            )}
          </div>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className="px-3 pb-3 pt-2 space-y-3">
            <p className="text-xs text-muted-foreground">
              Orden {node.executionOrder}
              {node.baseScore !== undefined ? ` · ${node.baseScore} pts` : ""}
            </p>
            <div className="space-y-1.5 max-w-[12rem]">
              <Label htmlFor={`base-score-${node.id}`} className="text-xs">
                Puntaje base
              </Label>
              <Input
                id={`base-score-${node.id}`}
                type="number"
                min={1}
                className="h-8 text-xs"
                value={node.baseScore ?? 100}
                disabled={isStructureLocked}
                onChange={(e) =>
                  onNodeChange(node.id, { baseScore: parseInt(e.target.value, 10) || 0 })
                }
              />
            </div>
            {isTrivia && (
              <TriviaNodeForm
                questions={node.questions ?? []}
                onChange={(qs: TriviaQuestion[]) => onNodeChange(node.id, { questions: qs })}
                isImmutable={isStructureLocked}
              />
            )}
            {isTreasure && (
              <TreasureHuntForm
                node={node}
                isImmutable={isStructureLocked}
                onChange={(patch) => onNodeChange(node.id, patch)}
              />
            )}
            {!isStructureLocked && (
              <Button
                size="sm"
                variant="secondary"
                className="h-7 text-xs"
                disabled={isSaving}
                onClick={() => void onSave(node.id)}
              >
                {isSaving ? "Guardando…" : isTrivia ? "Guardar trivia" : "Guardar búsqueda"}
              </Button>
            )}
            <HintPanel
              missionId={missionId}
              missionStatus={missionStatus}
              node={node}
              onHintsChange={onHintsChange}
            />
          </div>
        </CollapsibleContent>
      </Collapsible>
      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Eliminar juego</AlertDialogTitle>
            <AlertDialogDescription>
              Se eliminará este {isTrivia ? "reto de trivia" : "reto de búsqueda"}. Esta acción no se
              puede deshacer.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={isDeleting}
              onClick={() => {
                onDelete(node.id);
                setDeleteOpen(false);
              }}
            >
              {isDeleting ? "Eliminando…" : "Eliminar"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Stage Node Card ───────────────────────────────────────────────────────────

interface StageNodeCardProps {
  missionId: string;
  missionStatus: Mission["status"];
  stage: MissionNode;
  isStructureLocked: boolean;
  structureLockTooltip: string;
  isSaving: boolean;
  isDeleting: boolean;
  onStageChange: (stageId: string, patch: Partial<MissionNode>) => void;
  onSaveStage: (stageId: string) => Promise<void>;
  onDeleteStage: (stageId: string) => void;
  onOpenAddGame: () => void;
  onSaveGame: (gameId: string) => Promise<void>;
  onDeleteGame: (gameId: string) => void;
  savingGameId: string | null;
  deletingGameId: string | null;
  onNodeChange: (nodeId: string, patch: Partial<MissionNode>) => void;
  onHintsChange: (nodeId: string, hints: Hint[]) => void;
}

function StageNodeCard({
  missionId,
  missionStatus,
  stage,
  isStructureLocked,
  structureLockTooltip,
  isSaving,
  isDeleting,
  onStageChange,
  onSaveStage,
  onDeleteStage,
  onOpenAddGame,
  onSaveGame,
  onDeleteGame,
  savingGameId,
  deletingGameId,
  onNodeChange,
  onHintsChange,
}: StageNodeCardProps) {
  const [expanded, setExpanded] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const children = stage.children ?? [];

  return (
    <div className="rounded-xl border border-border bg-card">
      <Collapsible open={expanded} onOpenChange={setExpanded}>
        <CollapsibleTrigger asChild>
          <div
            className={cn(
              "flex items-center gap-3 px-4 py-3 cursor-pointer select-none bg-muted/40 hover:bg-muted/60 transition-colors rounded-xl",
              expanded && "rounded-b-none",
            )}
          >
            {expanded ? (
              <ChevronDownIcon className="h-4 w-4 text-muted-foreground shrink-0" />
            ) : (
              <ChevronRightIcon className="h-4 w-4 text-muted-foreground shrink-0" />
            )}
            <Layers3Icon className="h-4 w-4 text-foreground shrink-0" />
            <div className="flex-1 min-w-0">
              <span className="text-sm font-semibold text-foreground">{stage.title}</span>
              {stage.description && (
                <p className="text-xs text-muted-foreground truncate mt-0.5">{stage.description}</p>
              )}
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <span className="text-xs text-muted-foreground">Orden {stage.executionOrder}</span>
              <span className="text-xs text-muted-foreground">·</span>
              <span className="text-xs text-muted-foreground">
                {children.length} juego{children.length !== 1 ? "s" : ""}
              </span>
              {!isStructureLocked && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-destructive hover:text-destructive"
                  disabled={isDeleting}
                  onClick={(e) => {
                    e.stopPropagation();
                    setDeleteOpen(true);
                  }}
                >
                  {isDeleting ? (
                    <Loader2Icon className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Trash2Icon className="h-3.5 w-3.5" />
                  )}
                  <span className="sr-only">Eliminar etapa</span>
                </Button>
              )}
            </div>
          </div>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className="px-4 pb-4 pt-3 space-y-3">
            {!isStructureLocked && (
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs">Título de etapa</Label>
                  <Input
                    value={stage.title}
                    onChange={(e) => onStageChange(stage.id, { title: e.target.value })}
                    className="text-xs h-7"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Descripción</Label>
                  <Input
                    value={stage.description}
                    onChange={(e) => onStageChange(stage.id, { description: e.target.value })}
                    className="text-xs h-7"
                  />
                </div>
                <div className="col-span-2">
                  <Button
                    size="sm"
                    variant="secondary"
                    className="h-7 text-xs"
                    disabled={isSaving}
                    onClick={() => void onSaveStage(stage.id)}
                  >
                    {isSaving ? "Guardando…" : "Guardar etapa"}
                  </Button>
                </div>
              </div>
            )}
            {children.length > 0 && (
              <div className="space-y-2 mt-1">
                <p className="text-xs font-medium text-muted-foreground">Juegos ({children.length})</p>
                {children
                  .sort((a, b) => a.executionOrder - b.executionOrder)
                  .map((child) => (
                    <GameNodeCard
                      key={child.id}
                      missionId={missionId}
                      missionStatus={missionStatus}
                      node={child}
                      isStructureLocked={isStructureLocked}
                      isSaving={savingGameId === child.id}
                      isDeleting={deletingGameId === child.id}
                      onNodeChange={onNodeChange}
                      onHintsChange={onHintsChange}
                      onSave={onSaveGame}
                      onDelete={onDeleteGame}
                    />
                  ))}
              </div>
            )}
            <LockedAction locked={isStructureLocked} tooltip={structureLockTooltip}>
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5 text-xs h-8 mt-1"
                disabled={isStructureLocked}
                onClick={onOpenAddGame}
              >
                <PlusIcon className="h-3.5 w-3.5" />
                Añadir juego
              </Button>
            </LockedAction>
          </div>
        </CollapsibleContent>
      </Collapsible>
      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Eliminar etapa</AlertDialogTitle>
            <AlertDialogDescription>
              Se eliminará la etapa &ldquo;{stage.title}&rdquo; y su estructura asociada. Esta acción
              no se puede deshacer.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={isDeleting}
              onClick={() => {
                onDeleteStage(stage.id);
                setDeleteOpen(false);
              }}
            >
              {isDeleting ? "Eliminando…" : "Eliminar"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Mission Builder ───────────────────────────────────────────────────────────

export function MissionBuilder({ mission, onBack, onMissionChange }: MissionBuilderProps) {
  const [addStageOpen, setAddStageOpen] = useState(false);
  const [nodes, setNodes] = useState<MissionNode[]>(mission.nodes ?? []);
  const [isLoadingNodes, setIsLoadingNodes] = useState(true);
  const [nodesError, setNodesError] = useState<string | null>(null);
  const [isSubmittingStage, setIsSubmittingStage] = useState(false);
  const [savingStageId, setSavingStageId] = useState<string | null>(null);
  const [deletingStageId, setDeletingStageId] = useState<string | null>(null);
  const [addGameStageId, setAddGameStageId] = useState<string | null>(null);
  const [pendingGameType, setPendingGameType] = useState<MissionNodeType | null>(null);
  const [isCreatingGame, setIsCreatingGame] = useState(false);
  const [savingGameId, setSavingGameId] = useState<string | null>(null);
  const [deletingGameId, setDeletingGameId] = useState<string | null>(null);

  const isStructureLocked = mission.status !== "Draft";
  const structureLockTooltip = getStructureLockTooltip(mission.status);

  const stages = nodes
    .filter((n) => n.type === "Stage")
    .sort((a, b) => a.executionOrder - b.executionOrder);

  const missionRef = useRef(mission);
  missionRef.current = mission;

  const applyNodes = useCallback(
    (nextNodes: MissionNode[]) => {
      setNodes(nextNodes);
      onMissionChange({ ...missionRef.current, nodes: nextNodes });
    },
    [onMissionChange],
  );

  const loadNodes = useCallback(async (signal?: AbortSignal) => {
    setIsLoadingNodes(true);
    setNodesError(null);
    try {
      const dtos = await nodeService.getNodesByMission(mission.id, signal);
      if (signal?.aborted) return;
      const apiStages = sortStagesByExecutionOrder(
        dtos.map((dto) => toStageViewModel(dto, mission.id)),
      );
      const stagesWithGames = await Promise.all(
        apiStages.map(async (stage) => ({
          ...stage,
          children: await loadStageGames(mission.id, stage.id, signal),
        })),
      );
      if (signal?.aborted) return;
      setNodes(stagesWithGames);
    } catch (error) {
      if (signal?.aborted) return;
      logNodeApiProblem(error);
      const message = getNodeApiErrorMessage(error);
      setNodesError(message);
      toast.error(message);
    } finally {
      if (!signal?.aborted) {
        setIsLoadingNodes(false);
      }
    }
  }, [mission.id]);

  useEffect(() => {
    const controller = new AbortController();
    void loadNodes(controller.signal);
    return () => controller.abort();
  }, [loadNodes]);

  const updateNodeDeep = (
    nodeList: MissionNode[],
    nodeId: string,
    patch: Partial<MissionNode>,
  ): MissionNode[] =>
    nodeList.map((n) => {
      if (n.id === nodeId) return { ...n, ...patch };
      if (n.children) return { ...n, children: updateNodeDeep(n.children, nodeId, patch) };
      return n;
    });

  const updateHintsDeep = (
    nodeList: MissionNode[],
    nodeId: string,
    hints: Hint[],
  ): MissionNode[] =>
    nodeList.map((n) => {
      if (n.id === nodeId) return { ...n, hints };
      if (n.children) return { ...n, children: updateHintsDeep(n.children, nodeId, hints) };
      return n;
    });

  const handleStageChange = (stageId: string, patch: Partial<MissionNode>) => {
    applyNodes(updateNodeDeep(nodes, stageId, patch));
  };

  const handleNodeChange = (nodeId: string, patch: Partial<MissionNode>) => {
    applyNodes(updateNodeDeep(nodes, nodeId, patch));
  };

  const handleHintsChange = useCallback((nodeId: string, hints: Hint[]) => {
    setNodes((prev) => {
      const next = updateHintsDeep(prev, nodeId, hints);
      onMissionChange({ ...missionRef.current, nodes: next });
      return next;
    });
  }, [onMissionChange]);

  const handleAddStage = async (title: string, description: string, executionOrder: number) => {
    const existingOrders = stages.map((s) => s.executionOrder);
    if (existingOrders.includes(executionOrder)) {
      const message = "Ya existe una etapa con ese orden de ejecución.";
      toast.error(message);
      return;
    }

    setIsSubmittingStage(true);
    try {
      const { id } = await nodeService.addRootNode(mission.id, {
        title,
        description,
        executionOrder,
      });
      const newStage: MissionNode = {
        id,
        missionId: mission.id,
        type: "Stage",
        title,
        description,
        executionOrder,
        children: [],
        hints: [],
      };
      applyNodes(sortStagesByExecutionOrder([...nodes, newStage]));
      setAddStageOpen(false);
      toast.success("Etapa creada correctamente.");
    } catch (error) {
      logNodeApiProblem(error);
      const message = getNodeApiErrorMessage(error);
      toast.error(message);
    } finally {
      setIsSubmittingStage(false);
    }
  };

  const handleSaveStage = async (stageId: string) => {
    const stage = nodes.find((n) => n.id === stageId && n.type === "Stage");
    if (!stage) return;

    setSavingStageId(stageId);
    try {
      await nodeService.updateNode(mission.id, stageId, {
        title: stage.title,
        description: stage.description,
      });
      toast.success("Etapa actualizada.");
    } catch (error) {
      logNodeApiProblem(error);
      toast.error(getNodeApiErrorMessage(error));
    } finally {
      setSavingStageId(null);
    }
  };

  const handleDeleteStage = async (stageId: string) => {
    setDeletingStageId(stageId);
    try {
      await nodeService.deleteNode(mission.id, stageId);
      applyNodes(nodes.filter((n) => n.id !== stageId));
      toast.success("Etapa eliminada.");
    } catch (error) {
      logNodeApiProblem(error);
      toast.error(getNodeApiErrorMessage(error));
    } finally {
      setDeletingStageId(null);
    }
  };

  const findGameNode = (gameId: string): { stageId: string; game: MissionNode } | null => {
    for (const stage of nodes) {
      const game = stage.children?.find((c) => c.id === gameId);
      if (game) return { stageId: stage.id, game };
    }
    return null;
  };

  const appendGameToStage = (stageId: string, game: MissionNode) => {
    const updated = nodes.map((n) =>
      n.id === stageId
        ? {
            ...n,
            children: [...(n.children ?? []), game].sort(
              (a, b) => a.executionOrder - b.executionOrder,
            ),
          }
        : n,
    );
    applyNodes(updated);
  };

  const handleCreateTrivia = async (
    stageId: string,
    questions: TriviaQuestion[],
    executionOrder: number,
    baseScore: number,
  ) => {
    const validationError = validateTriviaQuestions(questions);
    if (validationError) {
      toast.error(validationError);
      return;
    }
    const scoreError = validateBaseScore(baseScore);
    if (scoreError) {
      toast.error(scoreError);
      return;
    }

    const stage = nodes.find((n) => n.id === stageId);
    const existingOrders = (stage?.children ?? []).map((c) => c.executionOrder);
    if (existingOrders.includes(executionOrder)) {
      toast.error("Ya existe un juego con ese orden en la etapa.");
      return;
    }

    setIsCreatingGame(true);
    try {
      const { id } = await gameService.addTrivia(mission.id, stageId, {
        questions: toApiTriviaQuestions(questions),
        executionOrder,
        baseScore,
      });
      const detail = await gameService.getTrivia(mission.id, id);
      appendGameToStage(stageId, toTriviaGameViewModel(detail, mission.id));
      setPendingGameType(null);
      setAddGameStageId(null);
      toast.success("Trivia creada.");
    } catch (error) {
      logGameApiProblem(error);
      toast.error(getGameApiErrorMessage(error));
    } finally {
      setIsCreatingGame(false);
    }
  };

  const handleCreateTreasureHunt = async (
    stageId: string,
    payload: {
      instructions: string;
      secretCode: string;
      destination: { latitude: number; longitude: number };
      executionOrder: number;
      baseScore: number;
    },
  ) => {
    const validationError = validateTreasureHuntPayload(payload);
    if (validationError) {
      toast.error(validationError);
      return;
    }
    const scoreError = validateBaseScore(payload.baseScore);
    if (scoreError) {
      toast.error(scoreError);
      return;
    }

    const stage = nodes.find((n) => n.id === stageId);
    const existingOrders = (stage?.children ?? []).map((c) => c.executionOrder);
    if (existingOrders.includes(payload.executionOrder)) {
      toast.error("Ya existe un juego con ese orden en la etapa.");
      return;
    }

    setIsCreatingGame(true);
    try {
      const { id } = await gameService.addTreasureHunt(mission.id, stageId, payload);
      const detail = await gameService.getTreasureHunt(mission.id, id);
      appendGameToStage(stageId, toTreasureHuntGameViewModel(detail, mission.id));
      setPendingGameType(null);
      setAddGameStageId(null);
      toast.success("Búsqueda del tesoro creada.");
    } catch (error) {
      logGameApiProblem(error);
      toast.error(getGameApiErrorMessage(error));
    } finally {
      setIsCreatingGame(false);
    }
  };

  const handleSaveGame = async (gameId: string) => {
    const located = findGameNode(gameId);
    if (!located) return;

    const { game } = located;
    setSavingGameId(gameId);
    try {
      const scoreError = validateBaseScore(game.baseScore ?? 0);
      if (scoreError) {
        toast.error(scoreError);
        return;
      }

      if (game.type === "Trivia") {
        const validationError = validateTriviaQuestions(game.questions ?? []);
        if (validationError) {
          toast.error(validationError);
          return;
        }
        await gameService.updateTrivia(mission.id, gameId, {
          questions: toApiTriviaQuestions(game.questions ?? []),
          baseScore: game.baseScore ?? 0,
        });
        const detail = await gameService.getTrivia(mission.id, gameId);
        handleNodeChange(gameId, toTriviaGameViewModel(detail, mission.id));
        toast.success("Trivia actualizada.");
      } else {
        const payload = {
          instructions: game.instructions ?? "",
          secretCode: game.secretCode ?? "",
          destination: game.destination ?? { latitude: 0, longitude: 0 },
          baseScore: game.baseScore ?? 0,
        };
        const validationError = validateTreasureHuntPayload(payload);
        if (validationError) {
          toast.error(validationError);
          return;
        }
        await gameService.updateTreasureHunt(mission.id, gameId, payload);
        const detail = await gameService.getTreasureHunt(mission.id, gameId);
        handleNodeChange(gameId, toTreasureHuntGameViewModel(detail, mission.id));
        toast.success("Búsqueda actualizada.");
      }
    } catch (error) {
      logGameApiProblem(error);
      toast.error(getGameApiErrorMessage(error));
    } finally {
      setSavingGameId(null);
    }
  };

  const handleDeleteGame = async (gameId: string) => {
    const located = findGameNode(gameId);
    if (!located) return;

    setDeletingGameId(gameId);
    try {
      await nodeService.deleteNode(mission.id, gameId);
      const updated = nodes.map((n) =>
        n.id === located.stageId
          ? { ...n, children: (n.children ?? []).filter((c) => c.id !== gameId) }
          : n,
      );
      applyNodes(updated);
      toast.success("Juego eliminado.");
    } catch (error) {
      logGameApiProblem(error);
      toast.error(getGameApiErrorMessage(error));
    } finally {
      setDeletingGameId(null);
    }
  };

  const addGameStage = addGameStageId ? nodes.find((n) => n.id === addGameStageId) : undefined;
  const childOrders = addGameStage?.children?.map((c) => c.executionOrder) ?? [];
  const nextGameOrder = childOrders.length === 0 ? 1 : Math.max(...childOrders) + 1;

  const addStageButton = (
    <Button
      size="sm"
      className="gap-1.5 shrink-0"
      disabled={isStructureLocked || isSubmittingStage}
      onClick={() => setAddStageOpen(true)}
    >
      <PlusIcon className="h-4 w-4" />
      Añadir etapa
    </Button>
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start gap-3">
        <Button variant="ghost" size="icon" className="h-8 w-8 mt-0.5 shrink-0" onClick={onBack}>
          <ChevronLeftIcon className="h-4 w-4" />
          <span className="sr-only">Volver al catálogo</span>
        </Button>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h1 className="text-xl font-semibold text-foreground text-balance">{mission.title}</h1>
            <StatusBadge status={mission.status} />
            {isStructureLocked && (
              <span className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-full px-2.5 py-0.5 font-medium">
                Solo lectura
              </span>
            )}
          </div>
          <div className="flex items-center gap-3 mt-1">
            <p className="text-sm text-muted-foreground">{mission.description}</p>
            <DifficultyStars value={mission.difficulty} />
          </div>
        </div>
        <LockedAction locked={isStructureLocked} tooltip={structureLockTooltip}>
          {addStageButton}
        </LockedAction>
      </div>

      {nodesError && (
        <Alert variant="destructive">
          <AlertTriangleIcon className="h-4 w-4" />
          <AlertTitle>Error al cargar etapas</AlertTitle>
          <AlertDescription className="flex items-center justify-between gap-2">
            <span>{nodesError}</span>
            <Button variant="outline" size="sm" onClick={() => void loadNodes()}>
              Reintentar
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {isLoadingNodes ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={`stage-skel-${i}`} className="h-20 w-full rounded-xl" />
          ))}
        </div>
      ) : stages.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border bg-muted/20 py-16 flex flex-col items-center gap-3">
          <Layers3Icon className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">Aún no hay etapas.</p>
          <LockedAction locked={isStructureLocked} tooltip={structureLockTooltip}>
            <Button
              size="sm"
              variant="outline"
              disabled={isStructureLocked}
              onClick={() => setAddStageOpen(true)}
              className="gap-1.5"
            >
              <PlusIcon className="h-4 w-4" />
              Crear primera etapa
            </Button>
          </LockedAction>
        </div>
      ) : (
        <div className="space-y-3">
          {stages.map((stage) => (
            <StageNodeCard
              key={stage.id}
              missionId={mission.id}
              missionStatus={mission.status}
              stage={stage}
              isStructureLocked={isStructureLocked}
              structureLockTooltip={structureLockTooltip}
              isSaving={savingStageId === stage.id}
              isDeleting={deletingStageId === stage.id}
              onStageChange={handleStageChange}
              onSaveStage={handleSaveStage}
              onDeleteStage={(id) => void handleDeleteStage(id)}
              onOpenAddGame={() => setAddGameStageId(stage.id)}
              onSaveGame={handleSaveGame}
              onDeleteGame={(id) => void handleDeleteGame(id)}
              savingGameId={savingGameId}
              deletingGameId={deletingGameId}
              onNodeChange={handleNodeChange}
              onHintsChange={handleHintsChange}
            />
          ))}
        </div>
      )}

      <AddStageDialog
        open={addStageOpen}
        onClose={() => setAddStageOpen(false)}
        onAdd={handleAddStage}
        nextOrder={
          stages.length === 0
            ? 1
            : Math.max(...stages.map((s) => s.executionOrder)) + 1
        }
        isSubmitting={isSubmittingStage}
      />

      <AddGameTypeDialog
        open={addGameStageId !== null && pendingGameType === null}
        onClose={() => setAddGameStageId(null)}
        onSelect={(type) => {
          setPendingGameType(type);
        }}
      />

      {addGameStageId && pendingGameType === "Trivia" && (
        <CreateTriviaDialog
          open
          stageId={addGameStageId}
          nextOrder={nextGameOrder}
          isSubmitting={isCreatingGame}
          onClose={() => {
            setPendingGameType(null);
            setAddGameStageId(null);
          }}
          onCreate={handleCreateTrivia}
        />
      )}

      {addGameStageId && pendingGameType === "TreasureHunt" && (
        <CreateTreasureHuntDialog
          open
          stageId={addGameStageId}
          nextOrder={nextGameOrder}
          isSubmitting={isCreatingGame}
          onClose={() => {
            setPendingGameType(null);
            setAddGameStageId(null);
          }}
          onCreate={handleCreateTreasureHunt}
        />
      )}
    </div>
  );
}
