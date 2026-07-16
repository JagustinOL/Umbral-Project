"use client";

import { useEffect, useMemo } from "react";
import {
  MapContainer,
  Marker,
  TileLayer,
  useMap,
  useMapEvents,
} from "react-leaflet";
import L from "leaflet";
import type { GpsCoordinate } from "@/lib/types";

import "leaflet/dist/leaflet.css";

const DEFAULT_CENTER: GpsCoordinate = { latitude: 10.496, longitude: -66.899 };
const DEFAULT_ZOOM = 16;

const markerIcon = L.icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

function isUnsetCoordinate(coord: GpsCoordinate): boolean {
  return coord.latitude === 0 && coord.longitude === 0;
}

function MapClickHandler({
  disabled,
  onChange,
}: {
  disabled: boolean;
  onChange: (coord: GpsCoordinate) => void;
}) {
  useMapEvents({
    click(e) {
      if (disabled) return;
      onChange({
        latitude: Number(e.latlng.lat.toFixed(6)),
        longitude: Number(e.latlng.lng.toFixed(6)),
      });
    },
  });
  return null;
}

function MapSync({
  coordinate,
  hasMarker,
}: {
  coordinate: GpsCoordinate;
  hasMarker: boolean;
}) {
  const map = useMap();

  useEffect(() => {
    const timer = window.setTimeout(() => map.invalidateSize(), 120);
    return () => window.clearTimeout(timer);
  }, [map]);

  useEffect(() => {
    if (!hasMarker) {
      map.setView([DEFAULT_CENTER.latitude, DEFAULT_CENTER.longitude], DEFAULT_ZOOM);
      return;
    }
    map.setView([coordinate.latitude, coordinate.longitude], map.getZoom(), {
      animate: true,
    });
  }, [coordinate.latitude, coordinate.longitude, hasMarker, map]);

  return null;
}

interface TreasureDestinationMapProps {
  destination: GpsCoordinate;
  disabled?: boolean;
  onChange: (coord: GpsCoordinate) => void;
}

export function TreasureDestinationMap({
  destination,
  disabled = false,
  onChange,
}: TreasureDestinationMapProps) {
  const hasMarker = !isUnsetCoordinate(destination);
  const center = useMemo(
    () =>
      hasMarker
        ? ([destination.latitude, destination.longitude] as [number, number])
        : ([DEFAULT_CENTER.latitude, DEFAULT_CENTER.longitude] as [number, number]),
    [destination.latitude, destination.longitude, hasMarker],
  );

  return (
    <div className="rounded-md border border-border isolate">
      <MapContainer
        center={center}
        zoom={DEFAULT_ZOOM}
        className="z-0 h-[240px] w-full"
        scrollWheelZoom={false}
        dragging={!disabled}
        doubleClickZoom={!disabled}
        zoomControl={!disabled}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <MapClickHandler disabled={disabled} onChange={onChange} />
        <MapSync coordinate={destination} hasMarker={hasMarker} />
        {hasMarker ? (
          <Marker
            position={[destination.latitude, destination.longitude]}
            icon={markerIcon}
            draggable={!disabled}
            eventHandlers={{
              dragend: (e) => {
                if (disabled) return;
                const latLng = e.target.getLatLng();
                onChange({
                  latitude: Number(latLng.lat.toFixed(6)),
                  longitude: Number(latLng.lng.toFixed(6)),
                });
              },
            }}
          />
        ) : null}
      </MapContainer>
      <p className="border-t border-border bg-muted/40 px-2 py-1 text-[10px] text-muted-foreground">
        {disabled
          ? "Mapa en solo lectura."
          : hasMarker
            ? "Haz clic en el mapa o arrastra el marcador. Usa los controles +/- para zoom."
            : "Haz clic en el mapa para fijar el destino GPS. Usa los controles +/- para zoom."}
      </p>
    </div>
  );
}
