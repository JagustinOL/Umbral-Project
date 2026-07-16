'use client';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

type ApplyPenaltyDialogProps = {
  open: boolean;
  teamName: string;
  isSubmitting?: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: (points: number, reason: string) => void | Promise<void>;
};

export function ApplyPenaltyDialog({
  open,
  teamName,
  isSubmitting = false,
  onOpenChange,
  onConfirm,
}: ApplyPenaltyDialogProps) {
  const [points, setPoints] = useState('10');
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setPoints('10');
    setReason('');
    setError(null);
  }, [open]);

  const handleSubmit = async () => {
    const parsedPoints = Number(points.trim());
    const trimmedReason = reason.trim();

    if (!Number.isInteger(parsedPoints) || parsedPoints <= 0) {
      setError('Indica un número entero de puntos mayor que cero.');
      return;
    }
    if (trimmedReason.length < 5) {
      setError('El motivo debe tener al menos 5 caracteres.');
      return;
    }

    setError(null);
    await onConfirm(parsedPoints, trimmedReason);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Aplicar penalización</DialogTitle>
          <DialogDescription>
            Sanción manual para <span className="font-medium text-foreground">{teamName}</span>.
            Se descontará del puntaje del equipo (RN-10).
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="penalty-points">Puntos a descontar</Label>
            <Input
              id="penalty-points"
              type="number"
              min={1}
              step={1}
              inputMode="numeric"
              value={points}
              disabled={isSubmitting}
              onChange={(e) => setPoints(e.target.value)}
              placeholder="Ej. 10"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="penalty-reason">Motivo</Label>
            <Textarea
              id="penalty-reason"
              value={reason}
              disabled={isSubmitting}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Describe la razón de la sanción…"
              rows={4}
              className="resize-none"
            />
            <p className="text-xs text-muted-foreground">Mínimo 5 caracteres.</p>
          </div>

          {error ? <p className="text-sm text-destructive">{error}</p> : null}
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={isSubmitting}
            onClick={() => onOpenChange(false)}
          >
            Cancelar
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={isSubmitting}
            onClick={() => void handleSubmit()}
          >
            {isSubmitting ? 'Aplicando…' : 'Aplicar sanción'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
