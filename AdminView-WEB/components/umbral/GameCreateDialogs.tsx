"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { TriviaNodeForm } from "./TriviaNodeForm";
import { TreasureHuntForm } from "./TreasureHuntForm";
import { GpsCoordinate, MissionNode, MissionNodeType, TriviaQuestion } from "@/lib/types";

const DEFAULT_BASE_SCORE = 100;

interface CreateTriviaDialogProps {
  open: boolean;
  stageId: string;
  nextOrder: number;
  isSubmitting: boolean;
  onClose: () => void;
  onCreate: (
    stageId: string,
    questions: TriviaQuestion[],
    executionOrder: number,
    baseScore: number,
  ) => Promise<void>;
}

export function CreateTriviaDialog({
  open,
  stageId,
  nextOrder,
  isSubmitting,
  onClose,
  onCreate,
}: CreateTriviaDialogProps) {
  const [questions, setQuestions] = useState<TriviaQuestion[]>([]);
  const [order, setOrder] = useState(nextOrder);
  const [baseScore, setBaseScore] = useState(DEFAULT_BASE_SCORE);

  useEffect(() => {
    if (open) {
      setOrder(nextOrder);
      setBaseScore(DEFAULT_BASE_SCORE);
      setQuestions([
        {
          id: "q-0",
          questionText: "",
          options: [
            { id: "o-0-0", text: "", isCorrect: true },
            { id: "o-0-1", text: "", isCorrect: false },
          ],
        },
      ]);
    }
  }, [open, nextOrder]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onCreate(stageId, questions, order, baseScore);
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Nueva trivia</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Cada pregunta debe tener exactamente una respuesta correcta.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 pt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="trivia-order">Orden de ejecución</Label>
              <Input
                id="trivia-order"
                type="number"
                min={1}
                value={order}
                onChange={(e) => setOrder(parseInt(e.target.value, 10) || 1)}
                disabled={isSubmitting}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="trivia-base-score">Puntaje base</Label>
              <Input
                id="trivia-base-score"
                type="number"
                min={1}
                value={baseScore}
                onChange={(e) => setBaseScore(parseInt(e.target.value, 10) || 0)}
                disabled={isSubmitting}
              />
            </div>
          </div>
          <TriviaNodeForm questions={questions} onChange={setQuestions} isImmutable={false} />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creando…" : "Crear trivia"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface CreateTreasureHuntDialogProps {
  open: boolean;
  stageId: string;
  nextOrder: number;
  isSubmitting: boolean;
  onClose: () => void;
  onCreate: (
    stageId: string,
    payload: {
      instructions: string;
      secretCode: string;
      destination: GpsCoordinate;
      executionOrder: number;
      baseScore: number;
    },
  ) => Promise<void>;
}

export function CreateTreasureHuntDialog({
  open,
  stageId,
  nextOrder,
  isSubmitting,
  onClose,
  onCreate,
}: CreateTreasureHuntDialogProps) {
  const [order, setOrder] = useState(nextOrder);
  const [baseScore, setBaseScore] = useState(DEFAULT_BASE_SCORE);
  const [draft, setDraft] = useState<MissionNode>({
    id: "draft",
    missionId: "",
    type: "TreasureHunt",
    title: "Treasure Hunt",
    description: "",
    executionOrder: nextOrder,
    baseScore: DEFAULT_BASE_SCORE,
    instructions: "",
    secretCode: "",
    destination: { latitude: 0, longitude: 0 },
  });

  useEffect(() => {
    if (open) {
      setOrder(nextOrder);
      setBaseScore(DEFAULT_BASE_SCORE);
      setDraft((prev) => ({
        ...prev,
        executionOrder: nextOrder,
        baseScore: DEFAULT_BASE_SCORE,
        instructions: "",
        secretCode: "",
        destination: { latitude: 0, longitude: 0 },
      }));
    }
  }, [open, nextOrder]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onCreate(stageId, {
      instructions: draft.instructions ?? "",
      secretCode: draft.secretCode ?? "",
      destination: draft.destination ?? { latitude: 0, longitude: 0 },
      executionOrder: order,
      baseScore,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Nueva búsqueda</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Instrucciones, código secreto y coordenadas GPS son obligatorios. El QR se genera al escribir el código.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 pt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="th-order">Orden de ejecución</Label>
              <Input
                id="th-order"
                type="number"
                min={1}
                value={order}
                onChange={(e) => setOrder(parseInt(e.target.value, 10) || 1)}
                disabled={isSubmitting}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="th-base-score">Puntaje base</Label>
              <Input
                id="th-base-score"
                type="number"
                min={1}
                value={baseScore}
                onChange={(e) => setBaseScore(parseInt(e.target.value, 10) || 0)}
                disabled={isSubmitting}
              />
            </div>
          </div>
          <TreasureHuntForm
            node={draft}
            isImmutable={false}
            onChange={(patch) => setDraft((prev) => ({ ...prev, ...patch }))}
          />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creando…" : "Crear búsqueda"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface AddGameTypeDialogProps {
  open: boolean;
  onClose: () => void;
  onSelect: (type: MissionNodeType) => void;
}

export function AddGameTypeDialog({ open, onClose, onSelect }: AddGameTypeDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Añadir juego</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Elige el tipo de juego para esta etapa.
          </DialogDescription>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-3 py-2">
          <button
            type="button"
            onClick={() => onSelect("Trivia")}
            className="flex flex-col items-center gap-2 rounded-lg border-2 border-border hover:border-foreground bg-card p-4 transition-colors"
          >
            <span className="text-sm font-medium text-foreground">Trivia</span>
            <span className="text-xs text-muted-foreground text-center">Validación automática</span>
          </button>
          <button
            type="button"
            onClick={() => onSelect("TreasureHunt")}
            className="flex flex-col items-center gap-2 rounded-lg border-2 border-border hover:border-foreground bg-card p-4 transition-colors"
          >
            <span className="text-sm font-medium text-foreground">Búsqueda</span>
            <span className="text-xs text-muted-foreground text-center">GPS + código QR</span>
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
