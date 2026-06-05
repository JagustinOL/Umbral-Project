'use client';

import { InfoIcon } from 'lucide-react';

interface InfoAlertProps {
  title: string;
  description: string;
}

export function InfoAlert({ title, description }: InfoAlertProps) {
  return (
    <div className="mt-6 flex gap-3 rounded-lg border border-border bg-muted/40 px-4 py-3">
      <InfoIcon className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5" />
      <div>
        <p className="text-sm font-medium text-foreground">{title}</p>
        <p className="text-sm text-muted-foreground mt-0.5">{description}</p>
      </div>
    </div>
  );
}
