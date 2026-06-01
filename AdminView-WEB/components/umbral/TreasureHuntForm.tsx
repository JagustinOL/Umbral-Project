"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { MissionNode, GpsCoordinate } from "@/lib/types";

interface TreasureHuntFormProps {
  node: MissionNode;
  isImmutable: boolean;
  onChange: (patch: Partial<MissionNode>) => void;
}

export function TreasureHuntForm({ node, isImmutable, onChange }: TreasureHuntFormProps) {
  const dest = node.destination ?? { latitude: 0, longitude: 0 };

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
              value={dest.latitude}
              type="number"
              step="any"
              onChange={(e) =>
                onChange({ destination: { ...dest, latitude: parseFloat(e.target.value) } })
              }
              placeholder="Latitude"
              className="text-xs h-8 font-mono"
              disabled={isImmutable}
            />
          </div>
          <div className="flex-1">
            <Input
              value={dest.longitude}
              type="number"
              step="any"
              onChange={(e) =>
                onChange({ destination: { ...dest, longitude: parseFloat(e.target.value) } })
              }
              placeholder="Longitude"
              className="text-xs h-8 font-mono"
              disabled={isImmutable}
            />
          </div>
        </div>
        <p className="text-xs text-muted-foreground">
          Lat: {dest.latitude.toFixed(4)}, Lng: {dest.longitude.toFixed(4)}
        </p>
      </div>
    </div>
  );
}
