"use client";

import { useState } from "react";
import {
  ChevronLeftIcon,
  PlusIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  MapPinIcon,
  BrainCircuitIcon,
  Layers3Icon,
  Trash2Icon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { StatusBadge } from "./StatusBadge";
import { DifficultyStars } from "./DifficultyStars";
import { HintPanel } from "./HintPanel";
import { TriviaNodeForm } from "./TriviaNodeForm";
import { TreasureHuntForm } from "./TreasureHuntForm";
import { Mission, MissionNode, Hint, MissionNodeType, TriviaQuestion } from "@/lib/types";
import { cn } from "@/lib/utils";

interface MissionBuilderProps {
  mission: Mission;
  onBack: () => void;
  onMissionChange: (updated: Mission) => void;
}

// ─── Add Stage Dialog ──────────────────────────────────────────────────────────

interface AddStageDialogProps {
  open: boolean;
  onClose: () => void;
  onAdd: (title: string, description: string, executionOrder: number) => void;
  nextOrder: number;
}

function AddStageDialog({ open, onClose, onAdd, nextOrder }: AddStageDialogProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [order, setOrder] = useState(nextOrder);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onAdd(title, description, order);
    setTitle("");
    setDescription("");
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Add Stage</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          <div className="space-y-1.5">
            <Label htmlFor="stage-title">Title <span className="text-destructive">*</span></Label>
            <Input id="stage-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Stage name…" required />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="stage-desc">Description</Label>
            <Textarea id="stage-desc" value={description} onChange={(e) => setDescription(e.target.value)} rows={2} placeholder="Stage description…" className="resize-none" />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="stage-order">Execution Order</Label>
            <Input id="stage-order" type="number" min={1} value={order} onChange={(e) => setOrder(parseInt(e.target.value))} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
            <Button type="submit">Add Stage</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Add Game Dialog ───────────────────────────────────────────────────────────

interface AddGameDialogProps {
  open: boolean;
  parentNodeId: string;
  onClose: () => void;
  onAdd: (type: MissionNodeType, parentId: string) => void;
}

function AddGameDialog({ open, parentNodeId, onClose, onAdd }: AddGameDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Add Game</DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-3 py-2">
          <button
            onClick={() => { onAdd("Trivia", parentNodeId); onClose(); }}
            className="flex flex-col items-center gap-2 rounded-lg border-2 border-border hover:border-foreground bg-card p-4 transition-colors group"
          >
            <BrainCircuitIcon className="h-7 w-7 text-muted-foreground group-hover:text-foreground" />
            <span className="text-sm font-medium text-foreground">Trivia</span>
            <span className="text-xs text-muted-foreground text-center">Q&A with auto-validation</span>
          </button>
          <button
            onClick={() => { onAdd("TreasureHunt", parentNodeId); onClose(); }}
            className="flex flex-col items-center gap-2 rounded-lg border-2 border-border hover:border-foreground bg-card p-4 transition-colors group"
          >
            <MapPinIcon className="h-7 w-7 text-muted-foreground group-hover:text-foreground" />
            <span className="text-sm font-medium text-foreground">Treasure Hunt</span>
            <span className="text-xs text-muted-foreground text-center">GPS + QR code</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ─── Game Node Card ────────────────────────────────────────────────────────────

interface GameNodeCardProps {
  node: MissionNode;
  isImmutable: boolean;
  onNodeChange: (nodeId: string, patch: Partial<MissionNode>) => void;
  onHintsChange: (nodeId: string, hints: Hint[]) => void;
  onRemove: (nodeId: string) => void;
}

function GameNodeCard({ node, isImmutable, onNodeChange, onHintsChange, onRemove }: GameNodeCardProps) {
  const [expanded, setExpanded] = useState(true);
  const isTrivia = node.type === "Trivia";
  const isTreasure = node.type === "TreasureHunt";

  return (
    <div className={cn(
      "rounded-lg border bg-card overflow-hidden",
      isTrivia ? "border-blue-200" : "border-amber-200"
    )}>
      <Collapsible open={expanded} onOpenChange={setExpanded}>
        <CollapsibleTrigger asChild>
          <div className={cn(
            "flex items-center gap-2 px-3 py-2.5 cursor-pointer select-none",
            isTrivia ? "bg-blue-50/50" : "bg-amber-50/50"
          )}>
            {expanded ? <ChevronDownIcon className="h-4 w-4 text-muted-foreground shrink-0" /> : <ChevronRightIcon className="h-4 w-4 text-muted-foreground shrink-0" />}
            {isTrivia ? (
              <BrainCircuitIcon className="h-4 w-4 text-blue-600 shrink-0" />
            ) : (
              <MapPinIcon className="h-4 w-4 text-amber-600 shrink-0" />
            )}
            <span className="text-sm font-medium text-foreground flex-1">{node.title}</span>
            <span className={cn(
              "text-xs px-2 py-0.5 rounded-full font-medium shrink-0",
              isTrivia ? "bg-blue-100 text-blue-700" : "bg-amber-100 text-amber-700"
            )}>
              {isTrivia ? "Trivia" : "Treasure Hunt"}
            </span>
            {!isImmutable && (
              <button
                onClick={(e) => { e.stopPropagation(); onRemove(node.id); }}
                className="text-muted-foreground hover:text-destructive transition-colors shrink-0"
                aria-label="Remove game"
              >
                <Trash2Icon className="h-3.5 w-3.5" />
              </button>
            )}
          </div>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className="px-3 pb-3 pt-2 space-y-3">
            {!isImmutable && (
              <div className="space-y-1">
                <Label className="text-xs">Title</Label>
                <Input
                  value={node.title}
                  onChange={(e) => onNodeChange(node.id, { title: e.target.value })}
                  className="text-xs h-7"
                />
              </div>
            )}
            {isTrivia && (
              <TriviaNodeForm
                questions={node.questions ?? []}
                onChange={(qs: TriviaQuestion[]) => onNodeChange(node.id, { questions: qs })}
                isImmutable={isImmutable}
              />
            )}
            {isTreasure && (
              <TreasureHuntForm
                node={node}
                isImmutable={isImmutable}
                onChange={(patch) => onNodeChange(node.id, patch)}
              />
            )}
            <HintPanel node={node} isImmutable={isImmutable} onHintsChange={onHintsChange} />
          </div>
        </CollapsibleContent>
      </Collapsible>
    </div>
  );
}

// ─── Stage Node Card ───────────────────────────────────────────────────────────

interface StageNodeCardProps {
  stage: MissionNode;
  isImmutable: boolean;
  onStageChange: (stageId: string, patch: Partial<MissionNode>) => void;
  onAddGame: (parentId: string, type: MissionNodeType) => void;
  onRemoveGame: (stageId: string, gameId: string) => void;
  onNodeChange: (nodeId: string, patch: Partial<MissionNode>) => void;
  onHintsChange: (nodeId: string, hints: Hint[]) => void;
}

function StageNodeCard({
  stage,
  isImmutable,
  onStageChange,
  onAddGame,
  onRemoveGame,
  onNodeChange,
  onHintsChange,
}: StageNodeCardProps) {
  const [expanded, setExpanded] = useState(true);
  const [addGameOpen, setAddGameOpen] = useState(false);
  const children = stage.children ?? [];

  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden">
      {/* Stage header */}
      <Collapsible open={expanded} onOpenChange={setExpanded}>
        <CollapsibleTrigger asChild>
          <div className="flex items-center gap-3 px-4 py-3 cursor-pointer select-none bg-muted/40 hover:bg-muted/60 transition-colors">
            {expanded ? <ChevronDownIcon className="h-4 w-4 text-muted-foreground shrink-0" /> : <ChevronRightIcon className="h-4 w-4 text-muted-foreground shrink-0" />}
            <Layers3Icon className="h-4 w-4 text-foreground shrink-0" />
            <div className="flex-1 min-w-0">
              <span className="text-sm font-semibold text-foreground">{stage.title}</span>
              {stage.description && (
                <p className="text-xs text-muted-foreground truncate mt-0.5">{stage.description}</p>
              )}
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <span className="text-xs text-muted-foreground">Order {stage.executionOrder}</span>
              <span className="text-xs text-muted-foreground">·</span>
              <span className="text-xs text-muted-foreground">{children.length} game{children.length !== 1 ? "s" : ""}</span>
            </div>
          </div>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className="px-4 pb-4 pt-3 space-y-3">
            {/* Stage description edit */}
            {!isImmutable && (
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs">Stage Title</Label>
                  <Input value={stage.title} onChange={(e) => onStageChange(stage.id, { title: e.target.value })} className="text-xs h-7" />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Description</Label>
                  <Input value={stage.description} onChange={(e) => onStageChange(stage.id, { description: e.target.value })} className="text-xs h-7" />
                </div>
              </div>
            )}
            {/* Stage hints */}
            <HintPanel node={stage} isImmutable={isImmutable} onHintsChange={onHintsChange} />
            {/* Child game nodes */}
            {children.length > 0 && (
              <div className="space-y-2 mt-1">
                <p className="text-xs font-medium text-muted-foreground">Games ({children.length})</p>
                {children
                  .sort((a, b) => a.executionOrder - b.executionOrder)
                  .map((child) => (
                    <GameNodeCard
                      key={child.id}
                      node={child}
                      isImmutable={isImmutable}
                      onNodeChange={onNodeChange}
                      onHintsChange={onHintsChange}
                      onRemove={(id) => onRemoveGame(stage.id, id)}
                    />
                  ))}
              </div>
            )}
            {!isImmutable && (
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5 text-xs h-8 mt-1"
                onClick={() => setAddGameOpen(true)}
              >
                <PlusIcon className="h-3.5 w-3.5" />
                Add Game
              </Button>
            )}
          </div>
        </CollapsibleContent>
      </Collapsible>
      <AddGameDialog
        open={addGameOpen}
        parentNodeId={stage.id}
        onClose={() => setAddGameOpen(false)}
        onAdd={(type, parentId) => onAddGame(parentId, type)}
      />
    </div>
  );
}

// ─── Mission Builder ───────────────────────────────────────────────────────────

export function MissionBuilder({ mission, onBack, onMissionChange }: MissionBuilderProps) {
  const [addStageOpen, setAddStageOpen] = useState(false);
  const isImmutable = mission.status === "Active";
  const stages = (mission.nodes ?? [])
    .filter((n) => n.type === "Stage")
    .sort((a, b) => a.executionOrder - b.executionOrder);

  // ── Node mutation helpers ──────────────────────────────────────────────────

  const updateNodeDeep = (nodes: MissionNode[], nodeId: string, patch: Partial<MissionNode>): MissionNode[] =>
    nodes.map((n) => {
      if (n.id === nodeId) return { ...n, ...patch };
      if (n.children) return { ...n, children: updateNodeDeep(n.children, nodeId, patch) };
      return n;
    });

  const updateHintsDeep = (nodes: MissionNode[], nodeId: string, hints: Hint[]): MissionNode[] =>
    nodes.map((n) => {
      if (n.id === nodeId) return { ...n, hints };
      if (n.children) return { ...n, children: updateHintsDeep(n.children, nodeId, hints) };
      return n;
    });

  const handleStageChange = (stageId: string, patch: Partial<MissionNode>) => {
    onMissionChange({ ...mission, nodes: updateNodeDeep(mission.nodes ?? [], stageId, patch) });
  };

  const handleNodeChange = (nodeId: string, patch: Partial<MissionNode>) => {
    onMissionChange({ ...mission, nodes: updateNodeDeep(mission.nodes ?? [], nodeId, patch) });
  };

  const handleHintsChange = (nodeId: string, hints: Hint[]) => {
    onMissionChange({ ...mission, nodes: updateHintsDeep(mission.nodes ?? [], nodeId, hints) });
  };

  const handleAddStage = (title: string, description: string, executionOrder: number) => {
    const existing = stages.map((s) => s.executionOrder);
    if (existing.includes(executionOrder)) return; // RN-11 conflict guard
    const newStage: MissionNode = {
      id: `n-${Date.now()}`,
      missionId: mission.id,
      type: "Stage",
      title,
      description,
      executionOrder,
      children: [],
      hints: [],
    };
    onMissionChange({ ...mission, nodes: [...(mission.nodes ?? []), newStage] });
    setAddStageOpen(false);
  };

  const handleAddGame = (parentId: string, type: MissionNodeType) => {
    const childOrder = (mission.nodes?.find((n) => n.id === parentId)?.children?.length ?? 0) + 1;
    const newGame: MissionNode = {
      id: `n-${Date.now()}`,
      missionId: mission.id,
      parentNodeId: parentId,
      type,
      title: type === "Trivia" ? "New Trivia" : "New Treasure Hunt",
      description: "",
      executionOrder: childOrder,
      hints: [],
      ...(type === "Trivia" ? { questions: [] } : { instructions: "", secretCode: "", destination: { latitude: 0, longitude: 0 } }),
    };
    const updated = (mission.nodes ?? []).map((n) =>
      n.id === parentId ? { ...n, children: [...(n.children ?? []), newGame] } : n
    );
    onMissionChange({ ...mission, nodes: updated });
  };

  const handleRemoveGame = (stageId: string, gameId: string) => {
    const updated = (mission.nodes ?? []).map((n) =>
      n.id === stageId ? { ...n, children: (n.children ?? []).filter((c) => c.id !== gameId) } : n
    );
    onMissionChange({ ...mission, nodes: updated });
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-start gap-3">
        <Button variant="ghost" size="icon" className="h-8 w-8 mt-0.5 shrink-0" onClick={onBack}>
          <ChevronLeftIcon className="h-4 w-4" />
          <span className="sr-only">Back to catalog</span>
        </Button>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h1 className="text-xl font-semibold text-foreground text-balance">{mission.title}</h1>
            <StatusBadge status={mission.status} />
            {isImmutable && (
              <span className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-full px-2.5 py-0.5 font-medium">
                RN-01: Read-only
              </span>
            )}
          </div>
          <div className="flex items-center gap-3 mt-1">
            <p className="text-sm text-muted-foreground">{mission.description}</p>
            <DifficultyStars value={mission.difficulty} />
          </div>
        </div>
        {!isImmutable && (
          <Button size="sm" className="gap-1.5 shrink-0" onClick={() => setAddStageOpen(true)}>
            <PlusIcon className="h-4 w-4" />
            Add Stage
          </Button>
        )}
      </div>

      {/* Stage list */}
      {stages.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border bg-muted/20 py-16 flex flex-col items-center gap-3">
          <Layers3Icon className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">No stages yet.</p>
          {!isImmutable && (
            <Button size="sm" variant="outline" onClick={() => setAddStageOpen(true)} className="gap-1.5">
              <PlusIcon className="h-4 w-4" />
              Add First Stage
            </Button>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {stages.map((stage) => (
            <StageNodeCard
              key={stage.id}
              stage={stage}
              isImmutable={isImmutable}
              onStageChange={handleStageChange}
              onAddGame={handleAddGame}
              onRemoveGame={handleRemoveGame}
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
        nextOrder={stages.length + 1}
      />
    </div>
  );
}
