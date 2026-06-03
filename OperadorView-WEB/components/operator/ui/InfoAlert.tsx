'use client';

import { AlertCircle } from 'lucide-react';

interface InfoAlertProps {
  title: string;
  description: string;
}

export function InfoAlert({ title, description }: InfoAlertProps) {
  return (
    <div className="p-4 bg-blue-950/40 border border-blue-800/60 rounded-lg">
      <div className="flex gap-3">
        <AlertCircle className="w-5 h-5 text-blue-400 flex-shrink-0 mt-0.5" />
        <div>
          <h3 className="font-semibold text-blue-200 mb-1">{title}</h3>
          <p className="text-sm text-blue-300/80">{description}</p>
        </div>
      </div>
    </div>
  );
}
