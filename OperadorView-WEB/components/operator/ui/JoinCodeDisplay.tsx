'use client';

import { Copy, Check } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface JoinCodeDisplayProps {
  code: string;
  onCopy: () => void;
  copied: boolean;
}

export function JoinCodeDisplay({ code, onCopy, copied }: JoinCodeDisplayProps) {
  return (
    <div className="p-8 bg-gradient-to-br from-slate-800 to-slate-900 border border-amber-600/30 rounded-lg">
      <p className="text-sm text-slate-400 uppercase tracking-wide mb-3">Join Code</p>
      <div className="flex items-center gap-4">
        <div className="flex-1">
          <p className="text-4xl font-mono font-bold text-amber-400 tracking-wider">{code}</p>
        </div>
        <Button
          onClick={onCopy}
          variant="outline"
          size="lg"
          className="border-amber-600 text-amber-400 hover:bg-amber-950 hover:text-amber-300"
        >
          {copied ? (
            <>
              <Check className="w-4 h-4 mr-2" />
              Copied!
            </>
          ) : (
            <>
              <Copy className="w-4 h-4 mr-2" />
              Copy
            </>
          )}
        </Button>
      </div>
      <p className="text-xs text-slate-400 mt-3">
        Teams can use this code to join the gaming session
      </p>
    </div>
  );
}
