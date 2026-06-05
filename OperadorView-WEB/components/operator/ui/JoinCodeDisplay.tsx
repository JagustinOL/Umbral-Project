'use client';

import { CheckIcon, CopyIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface JoinCodeDisplayProps {
  code: string;
  onCopy: () => void;
  copied: boolean;
}

export function JoinCodeDisplay({ code, onCopy, copied }: JoinCodeDisplayProps) {
  return (
    <div className="rounded-lg border border-border bg-card p-6">
      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-3">
        Código de unión
      </p>
      <div className="flex flex-col sm:flex-row sm:items-center gap-4">
        <p className="text-3xl font-mono font-semibold tracking-wider text-foreground">{code}</p>
        <Button onClick={onCopy} variant="outline" size="sm" className="shrink-0">
          {copied ? (
            <>
              <CheckIcon className="h-4 w-4 mr-2" />
              Copiado
            </>
          ) : (
            <>
              <CopyIcon className="h-4 w-4 mr-2" />
              Copiar
            </>
          )}
        </Button>
      </div>
      <p className="text-xs text-muted-foreground mt-3">
        Los equipos usan este código para unirse a la sesión
      </p>
    </div>
  );
}
