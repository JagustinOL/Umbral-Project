"use client";

import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Mission, CreateMissionPayload, UpdateMissionPayload } from "@/lib/types";

interface MissionFormModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateMissionPayload | UpdateMissionPayload) => void;
  mission?: Mission | null;
}

export function MissionFormModal({
  open,
  onClose,
  onSubmit,
  mission,
}: MissionFormModalProps) {
  const isEdit = !!mission;

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [difficulty, setDifficulty] = useState(1);
  const [maxDurationMinutes, setMaxDurationMinutes] = useState<string>("");

  useEffect(() => {
    if (mission) {
      setTitle(mission.title);
      setDescription(mission.description);
      setDifficulty(mission.difficulty);
      setMaxDurationMinutes(mission.maxDurationMinutes?.toString() ?? "");
    } else {
      setTitle("");
      setDescription("");
      setDifficulty(1);
      setMaxDurationMinutes("");
    }
  }, [mission, open]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      title,
      description,
      ...(isEdit ? {} : { difficulty }),
      maxDurationMinutes: maxDurationMinutes ? parseInt(maxDurationMinutes) : undefined,
    };
    onSubmit(payload);
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">
            {isEdit ? "Edit Mission" : "Create New Mission"}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          <div className="space-y-1.5">
            <Label htmlFor="mission-title">Title <span className="text-destructive">*</span></Label>
            <Input
              id="mission-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Operation: Silent Cipher"
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="mission-description">Description</Label>
            <Textarea
              id="mission-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Brief description of the mission…"
              rows={3}
            />
          </div>
          {!isEdit && (
            <div className="space-y-1.5">
              <Label htmlFor="mission-difficulty">
                Difficulty <span className="text-muted-foreground text-xs">(1–5)</span>
              </Label>
              <Input
                id="mission-difficulty"
                type="number"
                min={1}
                max={5}
                value={difficulty}
                onChange={(e) => setDifficulty(parseInt(e.target.value))}
                required
              />
            </div>
          )}
          <div className="space-y-1.5">
            <Label htmlFor="mission-duration">
              Max Duration{" "}
              <span className="text-muted-foreground text-xs">(minutes, optional)</span>
            </Label>
            <Input
              id="mission-duration"
              type="number"
              min={1}
              value={maxDurationMinutes}
              onChange={(e) => setMaxDurationMinutes(e.target.value)}
              placeholder="e.g. 90"
            />
          </div>
          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit">{isEdit ? "Save Changes" : "Create Mission"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
