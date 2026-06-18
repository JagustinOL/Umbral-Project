#!/usr/bin/env bash
# Levanta backends en Docker y arranca Expo en el host (Expo Go / emulador nativo).
# Para Player en navegador sin Node local: docker compose --profile full up -d --build

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "Levantando infraestructura y backends..."
docker compose up -d db mq keycloak mission-management-service session-management-service scoring-audit-service

echo "Esperando a que Keycloak este healthy (puede tardar 2-3 min la primera vez)..."
max_attempts=60
attempt=0
while [ "$attempt" -lt "$max_attempts" ]; do
  if docker compose ps keycloak 2>/dev/null | grep -q "(healthy)"; then
    echo "Keycloak listo."
    break
  fi
  sleep 5
  attempt=$((attempt + 1))
done
if [ "$attempt" -ge "$max_attempts" ]; then
  echo "Advertencia: Keycloak no reporto healthy a tiempo. Revisa: docker compose ps" >&2
fi

cd "$ROOT/PlayerMobile"

if [ ! -d node_modules ]; then
  echo "Instalando dependencias (npm ci)..."
  npm ci
fi

echo ""
echo "Iniciando Expo en el host (puerto 19000)..."
echo "Teléfono físico: sustituye localhost por la IPv4 de tu PC en PlayerMobile/.env"
echo "Emulador Android: usa 10.0.2.2 en lugar de localhost en .env"
echo ""

npm start
