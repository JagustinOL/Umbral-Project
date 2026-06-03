"use client";

import { useState } from "react";
import { PlusIcon, Trash2Icon, LightbulbIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Hint, MissionNode } from "@/lib/types";
import { cn } from "@/lib/utils";

interface HintPanelProps {
  node: MissionNode;
  isImmutable: boolean;
  onHintsChange: (nodeId: string, hints: Hint[]) => void;
}

export function HintPanel({ node, isImmutable, onHintsChange }: HintPanelProps) {
  const [newContent, setNewContent] = useState("");
  const hints = node.hints ?? [];

  const addHint = () => {
    if (!newContent.trim()) return;
    const newHint: Hint = {
      id: `h-${Date.now()}`,
      nodeId: node.id,
      content: newContent.trim(),
    };
    onHintsChange(node.id, [...hints, newHint]);
    setNewContent("");
  };

  const removeHint = (hintId: string) => {
    onHintsChange(node.id, hints.filter((h) => h.id !== hintId));
  };

  return (
    <div className="mt-3 space-y-2">
      <p className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
        <LightbulbIcon className="h-3.5 w-3.5" />
        Hints ({hints.length})
      </p>
      <div className="space-y-1.5">
        {hints.length === 0 && (
          <p className="text-xs text-muted-foreground italic py-1">No hints configured.</p>
        )}
        {hints.map((hint) => (
          <div
            key={hint.id}
            className="flex items-start gap-2 rounded-md border border-border bg-muted/30 px-3 py-2 text-xs"
          >
            <span className="flex-1 text-foreground leading-relaxed">{hint.content}</span>
            {!isImmutable && (
              <button
                onClick={() => removeHint(hint.id)}
                className="text-muted-foreground hover:text-destructive transition-colors shrink-0 mt-0.5"
                aria-label="Remove hint"
              >
                <Trash2Icon className="h-3 w-3" />
              </button>
            )}
          </div>
        ))}
      </div>
      {!isImmutable && (
        <div className="flex gap-2">
          <Textarea
            value={newContent}
            onChange={(e) => setNewContent(e.target.value)}
            placeholder="Add a hint…"
            rows={2}
            className="text-xs resize-none flex-1"
          />
          <Button
            size="sm"
            variant="outline"
            className="self-end h-8 gap-1 text-xs"
            onClick={addHint}
          >
            <PlusIcon className="h-3 w-3" />
            Add
          </Button>
        </div>
      )}
    </div>
  );
}
