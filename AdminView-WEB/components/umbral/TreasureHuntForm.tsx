"use client";

import dynamic from "next/dynamic";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { MissionNode, GpsCoordinate } from "@/lib/types";

const TreasureDestinationMap = dynamic(
  () =>
    import("./TreasureDestinationMap").then((m) => m.TreasureDestinationMap),
  {
    ssr: false,
    loading: () => (
      <div className="flex h-[240px] items-center justify-center rounded-md border border-border bg-muted/30 text-xs text-muted-foreground">
        Cargando mapa…
      </div>
    ),
  },
);

interface TreasureHuntFormProps {
  node: MissionNode;
  isImmutable: boolean;
  onChange: (patch: Partial<MissionNode>) => void;
}

export function TreasureHuntForm({ node, isImmutable, onChange }: TreasureHuntFormProps) {
  const dest = node.destination ?? { latitude: 0, longitude: 0 };

  const updateDestination = (next: GpsCoordinate) => {
    onChange({ destination: next });
  };

  return (
    <div className="space-y-3">
      <div className="space-y-1">
        <Label className="text-xs">Instructions</Label>
        <Textarea
          value={node.instructions ?? ""}
          onChange={(e) => onChange({ instructions: e.target.value })}
          placeholder="Describe where to find the treasure…"
          rows={2}
          className="text-xs resize-none"
          disabled={isImmutable}
        />
      </div>
      <div className="space-y-1">
        <Label className="text-xs">Secret QR Code</Label>
        <Input
          value={node.secretCode ?? ""}
          onChange={(e) => onChange({ secretCode: e.target.value })}
          placeholder="e.g. UMBRAL-TK-7741"
          className="text-xs h-8 font-mono"
          disabled={isImmutable}
        />
      </div>
      <div className="space-y-1">
        <Label className="text-xs">GPS Destination</Label>
        <div className="flex gap-2">
          <div className="flex-1">
            <Input
              value={Number.isFinite(dest.latitude) ? dest.latitude : ""}
              type="number"
              step="any"
              onChange={(e) => {
                const latitude = parseFloat(e.target.value);
                updateDestination({
                  ...dest,
                  latitude: Number.isFinite(latitude) ? latitude : 0,
                });
              }}
              placeholder="Latitude"
              className="text-xs h-8 font-mono"
              disabled={isImmutable}
            />
          </div>
          <div className="flex-1">
            <Input
              value={Number.isFinite(dest.longitude) ? dest.longitude : ""}
              type="number"
              step="any"
              onChange={(e) => {
                const longitude = parseFloat(e.target.value);
                updateDestination({
                  ...dest,
                  longitude: Number.isFinite(longitude) ? longitude : 0,
                });
              }}
              placeholder="Longitude"
              className="text-xs h-8 font-mono"
              disabled={isImmutable}
            />
          </div>
        </div>
        <p className="text-xs text-muted-foreground">
          Lat: {dest.latitude.toFixed(4)}, Lng: {dest.longitude.toFixed(4)}
        </p>
        <TreasureDestinationMap
          destination={dest}
          disabled={isImmutable}
          onChange={updateDestination}
        />
      </div>
    </div>
  );
}
