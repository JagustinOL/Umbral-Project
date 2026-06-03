'use client';

import { Zap, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface StartSessionButtonProps {
  disabled: boolean;
  loading: boolean;
  onStart: () => void;
}

export function StartSessionButton({
  disabled,
  loading,
  onStart,
}: StartSessionButtonProps) {
  return (
    <Button
      onClick={onStart}
      disabled={disabled || loading}
      className={`w-full gap-2 font-semibold py-6 ${
        disabled
          ? 'bg-slate-700 text-slate-400 cursor-not-allowed'
          : 'bg-amber-600 hover:bg-amber-700 text-slate-50'
      }`}
    >
      {loading ? (
        <>
          <Loader2 className="w-4 h-4 animate-spin" />
          Starting...
        </>
      ) : (
        <>
          <Zap className="w-4 h-4" />
          Start Session (HU-50)
        </>
      )}
    </Button>
  );
}
