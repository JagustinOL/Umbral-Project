'use client';

import { Loader2Icon, ZapIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface StartSessionButtonProps {
  disabled: boolean;
  loading: boolean;
  onStart: () => void;
}

export function StartSessionButton({ disabled, loading, onStart }: StartSessionButtonProps) {
  return (
    <Button
      onClick={onStart}
      disabled={disabled || loading}
      className="w-full gap-2"
      size="lg"
    >
      {loading ? (
        <>
          <Loader2Icon className="h-4 w-4 animate-spin" />
          Iniciando…
        </>
      ) : (
        <>
          <ZapIcon className="h-4 w-4" />
          Iniciar sesión
        </>
      )}
    </Button>
  );
}
