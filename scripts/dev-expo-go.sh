#!/usr/bin/env bash
# Levanta backends en Docker y arranca Expo en el host (Expo Go / emulador nativo).
# Detecta la IPv4 LAN y la inyecta en Metro + PlayerMobile/.env para Expo Go en telefono.
# Para Player en navegador sin Node local: docker compose --profile full up -d --build

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

get_lan_ipv4() {
  local ip=""

  if command -v ip >/dev/null 2>&1; then
    ip="$(ip route get 1.1.1.1 2>/dev/null | awk '{for (i=1;i<=NF;i++) if ($i=="src") {print $(i+1); exit}}' || true)"
  fi

  if [[ -z "${ip}" ]] && command -v ipconfig >/dev/null 2>&1; then
    # macOS
    for iface in en0 en1; do
      ip="$(ipconfig getifaddr "$iface" 2>/dev/null || true)"
      [[ -n "${ip}" ]] && break
    done
  fi

  if [[ -z "${ip}" ]]; then
    ip="$(hostname -I 2>/dev/null | awk '{print $1}' || true)"
  fi

  if [[ -n "${ip}" && "${ip}" != "127.0.0.1" ]]; then
    echo "${ip}"
  fi
}

update_player_mobile_env_host() {
  local env_path="$1"
  local lan_ip="$2"

  if [[ ! -f "${env_path}" ]]; then
    if [[ -f "${env_path}.example" ]]; then
      cp "${env_path}.example" "${env_path}"
      echo "Creado PlayerMobile/.env desde .env.example"
    else
      echo "Advertencia: no existe PlayerMobile/.env ni .env.example; omitiendo ajuste de APIs." >&2
      return
    fi
  fi

  local tmp
  tmp="$(mktemp)"
  local changed=0

  while IFS= read -r line || [[ -n "${line}" ]]; do
    if [[ "${line}" =~ ^(EXPO_PUBLIC_[A-Z0-9_]+)=(https?)://([^/:]+)(:[0-9]+)?(.*)$ ]]; then
      local name="${BASH_REMATCH[1]}"
      local scheme="${BASH_REMATCH[2]}"
      local old_host="${BASH_REMATCH[3]}"
      local port="${BASH_REMATCH[4]}"
      local rest="${BASH_REMATCH[5]}"
      if [[ "${old_host}" != "${lan_ip}" ]]; then
        changed=1
        echo "${name}=${scheme}://${lan_ip}${port}${rest}"
      else
        echo "${line}"
      fi
    else
      echo "${line}"
    fi
  done < "${env_path}" > "${tmp}"

  if [[ "${changed}" -eq 1 ]]; then
    mv "${tmp}" "${env_path}"
    echo "PlayerMobile/.env actualizado con host ${lan_ip}"
  else
    rm -f "${tmp}"
    echo "PlayerMobile/.env ya usa host ${lan_ip}"
  fi
}

echo "Deteniendo PlayerMobile web en Docker (libera puerto 19000 para Metro)..."
docker compose stop player-mobile-web >/dev/null 2>&1 || true

echo "Levantando infraestructura y backends..."
docker compose up -d db mq keycloak user-service mission-management-service session-management-service scoring-audit-service api-gateway

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

LAN_IP="$(get_lan_ipv4 || true)"
if [[ -z "${LAN_IP}" ]]; then
  echo "Advertencia: no se pudo detectar IPv4 LAN. Expo usara 127.0.0.1 y el telefono no conectara." >&2
  echo "Define manualmente: export REACT_NATIVE_PACKAGER_HOSTNAME=TU.IP.LAN" >&2
else
  export REACT_NATIVE_PACKAGER_HOSTNAME="${LAN_IP}"
  update_player_mobile_env_host "$ROOT/PlayerMobile/.env" "${LAN_IP}"
  echo ""
  echo "IP LAN detectada: ${LAN_IP}"
  echo "Metro/QR deberia mostrar: exp://${LAN_IP}:19000"
  echo "PC y telefono deben estar en la misma Wi-Fi."
fi

echo ""
echo "Iniciando Expo en el host (puerto 19000)..."
echo ""

npm start
