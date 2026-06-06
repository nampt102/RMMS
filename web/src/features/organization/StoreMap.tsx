"use client";

import "leaflet/dist/leaflet.css";

import L from "leaflet";
import { useMemo } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";
import type { Store } from "./types";

// Vietnam centroid + country-level zoom as the empty/default view.
const VN_CENTER: [number, number] = [16.0, 107.9];
const VN_ZOOM = 5;

const ACTIVE_COLOR = "#16a34a"; // green-600 — matches the table's "Success" tag
const INACTIVE_COLOR = "#6b7280"; // gray-500 — matches the "Default" tag

const prefersReducedMotion =
  typeof window !== "undefined" &&
  window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true;

/** Status-colored CSS pin — avoids Leaflet's default-marker image bundling issue (ADR-010). */
function markerIcon(active: boolean): L.DivIcon {
  const color = active ? ACTIVE_COLOR : INACTIVE_COLOR;
  return L.divIcon({
    className: "",
    html: `<span style="display:block;width:16px;height:16px;border-radius:9999px;background:${color};border:2px solid #fff;box-shadow:0 1px 3px rgba(0,0,0,.4)"></span>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8],
    popupAnchor: [0, -10],
  });
}

/** Pans/zooms the map to fit all plotted stores whenever the set changes. */
function FitBounds({ points }: { points: [number, number][] }) {
  const map = useMap();
  useMemo(() => {
    if (points.length === 0) {
      map.setView(VN_CENTER, VN_ZOOM, { animate: !prefersReducedMotion });
      return;
    }
    const [first] = points;
    if (points.length === 1 && first) {
      map.setView(first, 15, { animate: !prefersReducedMotion });
      return;
    }
    map.fitBounds(L.latLngBounds(points), { padding: [40, 40], animate: !prefersReducedMotion });
  }, [points, map]);
  return null;
}

type StoreMapProps = {
  stores: Store[];
  statusLabel: (status: string) => string;
};

export default function StoreMap({ stores, statusLabel }: StoreMapProps) {
  const points = useMemo<[number, number][]>(
    () => stores.map((s) => [s.latitude, s.longitude]),
    [stores],
  );

  return (
    <MapContainer
      center={VN_CENTER}
      zoom={VN_ZOOM}
      scrollWheelZoom
      zoomAnimation={!prefersReducedMotion}
      markerZoomAnimation={!prefersReducedMotion}
      fadeAnimation={!prefersReducedMotion}
      style={{ height: "100%", width: "100%", borderRadius: 8 }}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        maxZoom={19}
      />
      <FitBounds points={points} />
      {stores.map((s) => (
        <Marker key={s.id} position={[s.latitude, s.longitude]} icon={markerIcon(s.status === "active")}>
          <Popup>
            <div style={{ minWidth: 160 }}>
              <div style={{ fontWeight: 600 }}>{s.name}</div>
              <div style={{ fontFamily: "monospace", color: "#475569" }}>{s.code}</div>
              {s.address ? <div style={{ color: "#475569", marginTop: 4 }}>{s.address}</div> : null}
              <div style={{ marginTop: 6 }}>
                <span
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 6,
                  }}
                >
                  <span
                    style={{
                      width: 10,
                      height: 10,
                      borderRadius: 9999,
                      background: s.status === "active" ? ACTIVE_COLOR : INACTIVE_COLOR,
                    }}
                  />
                  {statusLabel(s.status)}
                </span>
              </div>
              {s.areaName ? (
                <div style={{ color: "#475569", marginTop: 4 }}>{s.areaName}</div>
              ) : null}
              <div style={{ fontFamily: "monospace", color: "#94a3b8", marginTop: 4, fontSize: 12 }}>
                {s.latitude.toFixed(5)}, {s.longitude.toFixed(5)}
              </div>
            </div>
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
}
