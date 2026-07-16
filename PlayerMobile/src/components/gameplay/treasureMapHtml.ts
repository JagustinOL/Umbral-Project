export const TREASURE_AREA_RADIUS_METERS = 50;
export const TREASURE_MAP_HEIGHT = 220;

/** HTML embebido para WebView nativo (iOS/Android). */
export function buildTreasureMapHtml(
  latitude: number,
  longitude: number,
  radiusMeters: number = TREASURE_AREA_RADIUS_METERS,
  heightPx: number = TREASURE_MAP_HEIGHT,
): string {
  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/leaflet.css" />
  <script src="https://cdn.jsdelivr.net/npm/leaflet@1.9.4/dist/leaflet.js"></script>
  <style>
    html, body { margin: 0; padding: 0; width: 100%; height: ${heightPx}px; overflow: hidden; background: #e8edf4; }
    #map { width: 100%; height: ${heightPx}px; }
    .leaflet-control-attribution { font-size: 9px; }
  </style>
</head>
<body>
  <div id="map"></div>
  <script>
    (function () {
      var lat = ${Number(latitude)};
      var lng = ${Number(longitude)};
      var radius = ${Number(radiusMeters)};
      var map = L.map('map', {
        zoomControl: true,
        attributionControl: true
      }).setView([lat, lng], 17);

      L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20
      }).addTo(map);

      var circle = L.circle([lat, lng], {
        radius: radius,
        color: '#3d9eff',
        fillColor: '#3d9eff',
        fillOpacity: 0.25,
        weight: 2
      }).addTo(map);

      function refresh() {
        map.invalidateSize(true);
        map.fitBounds(circle.getBounds(), { padding: [28, 28], maxZoom: 18 });
      }
      window.mapRefresh = refresh;

      refresh();
      setTimeout(refresh, 50);
      setTimeout(refresh, 250);
      setTimeout(refresh, 600);
      window.addEventListener('load', refresh);
    })();
  </script>
</body>
</html>`;
}
