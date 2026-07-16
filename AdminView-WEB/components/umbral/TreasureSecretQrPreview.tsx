"use client";

import { useId, useRef } from "react";
import { QRCodeCanvas } from "qrcode.react";
import { Button } from "@/components/ui/button";

interface TreasureSecretQrPreviewProps {
  secretCode: string;
}

export function TreasureSecretQrPreview({ secretCode }: TreasureSecretQrPreviewProps) {
  const value = secretCode.trim();
  const reactId = useId();
  const canvasId = `treasure-qr-${reactId.replace(/:/g, "")}`;
  const wrapRef = useRef<HTMLDivElement>(null);

  const handleDownload = () => {
    const canvas =
      (document.getElementById(canvasId) as HTMLCanvasElement | null) ??
      wrapRef.current?.querySelector("canvas");
    if (!canvas || !value) return;

    const link = document.createElement("a");
    link.download = `umbral-treasure-${value.replace(/[^a-zA-Z0-9-_]/g, "_")}.png`;
    link.href = canvas.toDataURL("image/png");
    link.click();
  };

  if (!value) {
    return (
      <div className="flex min-h-[120px] items-center justify-center rounded-md border border-dashed border-border bg-muted/30 px-3 text-center text-xs text-muted-foreground">
        Escribe el código secreto para generar el QR imprimible.
      </div>
    );
  }

  return (
    <div
      ref={wrapRef}
      className="flex flex-col items-center gap-2 rounded-md border-2 border-amber-500/40 bg-amber-50/50 p-3 dark:bg-amber-950/20"
    >
      <p className="text-xs font-semibold text-foreground">Vista previa del QR</p>
      <div className="rounded-md bg-white p-3 shadow-sm">
        <QRCodeCanvas
          id={canvasId}
          value={value}
          size={160}
          level="M"
          marginSize={2}
          bgColor="#FFFFFF"
          fgColor="#000000"
        />
      </div>
      <p
        className="max-w-full truncate font-mono text-xs font-medium text-foreground"
        title={value}
      >
        {value}
      </p>
      <p className="text-center text-[11px] text-muted-foreground">
        Imprime o descarga este QR y colócalo en el punto físico del tesoro.
      </p>
      <Button type="button" size="sm" variant="secondary" className="h-8 text-xs" onClick={handleDownload}>
        Descargar QR (PNG)
      </Button>
    </div>
  );
}
