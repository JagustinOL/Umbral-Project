"use client";

import { useState } from "react";
import { PlusIcon, Trash2Icon, CheckCircle2Icon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TriviaQuestion, TriviaOption } from "@/lib/types";
import { cn } from "@/lib/utils";

interface TriviaNodeFormProps {
  questions: TriviaQuestion[];
  onChange: (questions: TriviaQuestion[]) => void;
  isImmutable: boolean;
}

export function TriviaNodeForm({ questions, onChange, isImmutable }: TriviaNodeFormProps) {
  const addQuestion = () => {
    const newQ: TriviaQuestion = {
      id: `q-${Date.now()}`,
      questionText: "",
      options: [
        { id: `o-${Date.now()}-1`, text: "", isCorrect: false },
        { id: `o-${Date.now()}-2`, text: "", isCorrect: false },
      ],
    };
    onChange([...questions, newQ]);
  };

  const removeQuestion = (qId: string) => {
    onChange(questions.filter((q) => q.id !== qId));
  };

  const updateQuestionText = (qId: string, text: string) => {
    onChange(questions.map((q) => (q.id === qId ? { ...q, questionText: text } : q)));
  };

  const addOption = (qId: string) => {
    onChange(
      questions.map((q) =>
        q.id === qId
          ? {
              ...q,
              options: [...q.options, { id: `o-${Date.now()}`, text: "", isCorrect: false }],
            }
          : q
      )
    );
  };

  const removeOption = (qId: string, oId: string) => {
    onChange(
      questions.map((q) =>
        q.id === qId ? { ...q, options: q.options.filter((o) => o.id !== oId) } : q
      )
    );
  };

  const updateOption = (qId: string, oId: string, field: keyof TriviaOption, value: string | boolean) => {
    onChange(
      questions.map((q) => {
        if (q.id !== qId) return q;
        const options =
          field === "isCorrect"
            ? q.options.map((o) => ({ ...o, isCorrect: o.id === oId }))
            : q.options.map((o) => (o.id === oId ? { ...o, [field]: value } : o));
        return { ...q, options };
      })
    );
  };

  return (
    <div className="space-y-4">
      {questions.length === 0 && (
        <p className="text-xs text-muted-foreground italic">No questions yet.</p>
      )}
      {questions.map((q, qi) => (
        <div key={q.id} className="rounded-md border border-border bg-background p-3 space-y-3">
          <div className="flex items-start gap-2">
            <span className="text-xs font-medium text-muted-foreground mt-2 shrink-0">Q{qi + 1}.</span>
            <Input
              value={q.questionText}
              onChange={(e) => updateQuestionText(q.id, e.target.value)}
              placeholder="Enter question text…"
              className="text-sm h-8 flex-1"
              disabled={isImmutable}
            />
            {!isImmutable && (
              <Button
                size="icon"
                variant="ghost"
                className="h-8 w-8 text-muted-foreground hover:text-destructive shrink-0"
                onClick={() => removeQuestion(q.id)}
              >
                <Trash2Icon className="h-3.5 w-3.5" />
                <span className="sr-only">Remove question</span>
              </Button>
            )}
          </div>
          <div className="space-y-1.5 pl-5">
            <p className="text-xs text-muted-foreground">Options — click to mark correct answer:</p>
            {q.options.map((opt) => (
              <div key={opt.id} className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={isImmutable}
                  onClick={() => updateOption(q.id, opt.id, "isCorrect", true)}
                  className={cn(
                    "shrink-0 transition-colors",
                    opt.isCorrect
                      ? "text-emerald-600"
                      : "text-muted-foreground hover:text-emerald-500"
                  )}
                  aria-label="Mark as correct"
                >
                  <CheckCircle2Icon className="h-4 w-4" />
                </button>
                <Input
                  value={opt.text}
                  onChange={(e) => updateOption(q.id, opt.id, "text", e.target.value)}
                  placeholder={`Option text…`}
                  className={cn(
                    "text-xs h-7 flex-1",
                    opt.isCorrect && "border-emerald-300 bg-emerald-50 text-emerald-800"
                  )}
                  disabled={isImmutable}
                />
                {!isImmutable && q.options.length > 2 && (
                  <Button
                    size="icon"
                    variant="ghost"
                    className="h-7 w-7 text-muted-foreground hover:text-destructive shrink-0"
                    onClick={() => removeOption(q.id, opt.id)}
                  >
                    <Trash2Icon className="h-3 w-3" />
                  </Button>
                )}
              </div>
            ))}
            {!isImmutable && (
              <Button
                size="sm"
                variant="ghost"
                className="h-6 text-xs gap-1 text-muted-foreground hover:text-foreground px-0"
                onClick={() => addOption(q.id)}
              >
                <PlusIcon className="h-3 w-3" />
                Add option
              </Button>
            )}
          </div>
        </div>
      ))}
      {!isImmutable && (
        <Button
          size="sm"
          variant="outline"
          className="gap-1.5 text-xs h-8"
          onClick={addQuestion}
        >
          <PlusIcon className="h-3.5 w-3.5" />
          Add Question
        </Button>
      )}
    </div>
  );
}
