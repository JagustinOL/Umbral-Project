"""Genera Umbral-Postman-Collection.json con variables y scripts de auto-guardado."""

import json
import uuid
from pathlib import Path

# --- Scripts Postman (Tests): guardan IDs en variables de colección ---

SCRIPT_SAVE_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) pm.collectionVariables.set('lastCreatedId', body.id);
}"""

SCRIPT_MISSION_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) pm.collectionVariables.set('missionId', body.id);
}"""

SCRIPT_STAGE_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) {
        pm.collectionVariables.set('nodeId', body.id);
        pm.collectionVariables.set('stageId', body.id);
        pm.collectionVariables.set('parentNodeId', body.id);
    }
}"""

SCRIPT_GAME_NODE_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) pm.collectionVariables.set('nodeId', body.id);
}"""

SCRIPT_HINT_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) pm.collectionVariables.set('hintId', body.id);
}"""

SCRIPT_SYNC_STAGES = """\
if (pm.response.code === 200) {
    const nodes = pm.response.json();
    if (Array.isArray(nodes) && nodes.length > 0) {
        const stage = nodes.find(n => n.nodeType === 'Stage') || nodes[0];
        if (stage.id) {
            pm.collectionVariables.set('stageId', stage.id);
            pm.collectionVariables.set('parentNodeId', stage.id);
        }
    }
}"""

SCRIPT_OPERATOR_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) pm.collectionVariables.set('operatorId', body.id);
}"""

SCRIPT_PLAYER_IDS = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) {
        pm.collectionVariables.set('playerId', body.id);
        pm.collectionVariables.set('creatorId', body.id);
        pm.collectionVariables.set('requestorId', body.id);
    }
}"""

SCRIPT_TEAM_ID = """\
if (pm.response.code === 201) {
    const body = pm.response.json();
    if (body.id) {
        pm.collectionVariables.set('teamId', body.id);
        if (!pm.collectionVariables.get('requestorId')) {
            pm.collectionVariables.set('requestorId', pm.collectionVariables.get('creatorId'));
        }
    }
}"""

SCRIPT_TEAM_CODE = """\
if (pm.response.code === 200) {
    const body = pm.response.json();
    if (body.teamCode) pm.collectionVariables.set('teamCode', body.teamCode);
}"""

SCRIPT_REQUEST_ID = """\
if (pm.response.code === 200) {
    const body = pm.response.json();
    if (body.requestId) pm.collectionVariables.set('requestId', body.requestId);
}"""

SCRIPT_FIRST_PENDING_REQUEST = """\
if (pm.response.code === 200) {
    const list = pm.response.json();
    if (Array.isArray(list) && list.length > 0 && list[0].id) {
        pm.collectionVariables.set('requestId', list[0].id);
    }
}"""

SCRIPT_SESSION = """\
if (pm.response.code === 200) {
    const body = pm.response.json();
    if (body.sessionId) pm.collectionVariables.set('sessionId', body.sessionId);
    if (body.joinCode) pm.collectionVariables.set('joinCode', body.joinCode);
}"""

SCRIPT_JOIN_SESSION = """\
if (pm.response.code === 200) {
    const body = pm.response.json();
    if (body.sessionId) pm.collectionVariables.set('sessionId', body.sessionId);
}"""

COLLECTION_PREREQUEST = """\
// Propaga creatorId → requestorId si falta (líder del equipo)
const requestorId = pm.collectionVariables.get('requestorId');
const creatorId = pm.collectionVariables.get('creatorId');
if (!requestorId && creatorId) {
    pm.collectionVariables.set('requestorId', creatorId);
}
// parentNodeId por defecto = etapa
const parentNodeId = pm.collectionVariables.get('parentNodeId');
const stageId = pm.collectionVariables.get('stageId');
if (!parentNodeId && stageId) {
    pm.collectionVariables.set('parentNodeId', stageId);
}"""


def uid() -> str:
    return str(uuid.uuid4())


def tests(script: str) -> list[dict]:
    lines = [line for line in script.strip().split("\n")]
    return [
        {
            "listen": "test",
            "script": {
                "type": "text/javascript",
                "exec": lines,
            },
        }
    ]


def build_url(path_suffix: str, service: str = "mission", query: dict | None = None) -> dict:
    base_var = (
        "{{missionManagementUrl}}"
        if service == "mission"
        else "{{sessionManagementUrl}}"
    )
    # Puertos Docker (docker-compose.yml): MM 5260, SM 5278
    # dotnet run local: MM 5117, SM 5240 — cambia las variables de colección si usas IDE
    port_var = (
        "{{missionManagementPort}}"
        if service == "mission"
        else "{{sessionManagementPort}}"
    )
    path_parts = [p for p in path_suffix.strip("/").split("/") if p]
    raw = f"{base_var}/{'/'.join(path_parts)}"

    url: dict = {
        "raw": raw,
        "protocol": "http",
        "host": ["localhost"],
        "port": port_var,
        "path": path_parts,
    }

    if query:
        raw += "?" + "&".join(f"{k}={v}" for k, v in query.items())
        url["raw"] = raw
        url["query"] = [{"key": k, "value": v, "disabled": False} for k, v in query.items()]

    return url


def req(
    name: str,
    method: str,
    path_suffix: str,
    body=None,
    query=None,
    formdata=None,
    service: str = "mission",
    test_script: str | None = None,
) -> dict:
    item: dict = {
        "name": name,
        "request": {
            "method": method,
            "header": [],
            "url": build_url(path_suffix, service, query),
        },
    }

    if body is not None:
        item["request"]["header"] = [{"key": "Content-Type", "value": "application/json"}]
        item["request"]["body"] = {
            "mode": "raw",
            "raw": json.dumps(body, indent=2, ensure_ascii=False),
        }
    if formdata:
        item["request"]["body"] = {"mode": "formdata", "formdata": formdata}
        item["request"]["header"] = []

    if test_script:
        item["event"] = tests(test_script)

    return item


def folder(name: str, items: list, desc: str = "") -> dict:
    return {"name": name, "description": desc, "item": items}


def main() -> None:
    e1 = [
        req(
            "1 · Crear Misión (HU-01)",
            "POST",
            "api/v1/missions",
            {
                "title": "Misión demo",
                "description": "Descripción",
                "difficulty": 1,
                "maxDurationMinutes": 60,
            },
            test_script=SCRIPT_MISSION_ID,
        ),
        req("Consultar Misiones (HU-02)", "GET", "api/v1/missions"),
        req("Consultar Misión por Id (HU-02)", "GET", "api/v1/missions/{{missionId}}"),
        req(
            "Modificar Misión (HU-03)",
            "PUT",
            "api/v1/missions/{{missionId}}",
            {
                "title": "Misión actualizada",
                "description": "Nueva descripción",
                "maxDurationMinutes": 90,
            },
        ),
        req("2 · Activar Misión", "PUT", "api/v1/missions/{{missionId}}/activate"),
        req("Desactivar Misión (HU-04)", "DELETE", "api/v1/missions/{{missionId}}"),
        req(
            "3 · Crear Etapa (HU-05)",
            "POST",
            "api/v1/missions/{{missionId}}/nodes",
            {
                "title": "Etapa 1",
                "description": "Primera etapa",
                "executionOrder": 1,
            },
            test_script=SCRIPT_STAGE_ID,
        ),
        req(
            "Consultar Etapas (HU-06)",
            "GET",
            "api/v1/missions/{{missionId}}/nodes",
            test_script=SCRIPT_SYNC_STAGES,
        ),
        req(
            "Listar Juegos de Etapa",
            "GET",
            "api/v1/missions/{{missionId}}/nodes/{{stageId}}/games",
        ),
        req(
            "Modificar Etapa (HU-07)",
            "PUT",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}",
            {"title": "Etapa actualizada", "description": "Desc"},
        ),
        req(
            "Eliminar Etapa (HU-08)",
            "DELETE",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}",
        ),
    ]

    e2 = [
        req(
            "4 · Añadir Trivia (HU-09)",
            "POST",
            "api/v1/missions/{{missionId}}/nodes/{{parentNodeId}}/trivia",
            {
                "questions": [
                    {
                        "prompt": "¿Capital de Venezuela?",
                        "options": ["Caracas", "Valencia", "Maracaibo"],
                        "correctOptionIndex": 0,
                    }
                ],
                "executionOrder": 1,
            },
            test_script=SCRIPT_GAME_NODE_ID,
        ),
        req(
            "Consultar Trivia (HU-10)",
            "GET",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/trivia",
        ),
        req(
            "Modificar Trivia (HU-11)",
            "PUT",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/trivia",
            {
                "questions": [
                    {
                        "prompt": "¿2 + 2?",
                        "options": ["3", "4", "5"],
                        "correctOptionIndex": 1,
                    }
                ]
            },
        ),
        req(
            "Eliminar Trivia (HU-12)",
            "DELETE",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}",
        ),
        req(
            "5 · Añadir Treasure Hunt (HU-13)",
            "POST",
            "api/v1/missions/{{missionId}}/nodes/{{parentNodeId}}/treasure-hunts",
            {
                "instructions": "Busca el punto",
                "secretCode": "CODE-123",
                "destination": {"latitude": 10.5, "longitude": -66.9},
                "executionOrder": 2,
            },
            test_script=SCRIPT_GAME_NODE_ID,
        ),
        req(
            "Consultar Treasure Hunt (HU-14)",
            "GET",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/treasure-hunts",
        ),
        req(
            "Modificar Treasure Hunt (HU-15)",
            "PUT",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/treasure-hunts",
            {
                "instructions": "Nuevas instrucciones",
                "secretCode": "CODE-456",
                "destination": {"latitude": 10.6, "longitude": -67.0},
            },
        ),
        req(
            "Eliminar Treasure Hunt (HU-16)",
            "DELETE",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}",
        ),
        req(
            "Crear Pista (HU-17)",
            "POST",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/hints",
            formdata=[
                {"key": "content", "value": "Contenido de la pista", "type": "text"},
                {"key": "attachment", "type": "file", "src": [], "disabled": True},
            ],
            test_script=SCRIPT_HINT_ID,
        ),
        req(
            "Consultar Pistas (HU-18)",
            "GET",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/hints",
        ),
        req(
            "Modificar Pista (HU-19)",
            "PUT",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/hints/{{hintId}}",
            {"content": "Pista actualizada"},
        ),
        req(
            "Eliminar Pista (HU-20)",
            "DELETE",
            "api/v1/missions/{{missionId}}/nodes/{{nodeId}}/hints/{{hintId}}",
        ),
    ]

    e3 = [
        req(
            "Crear Operador (HU-22)",
            "POST",
            "api/v1/operators",
            {
                "firstName": "Ana",
                "lastName": "Operador",
                "email": "ana@umbral.com",
                "password": "SecurePass1",
            },
            test_script=SCRIPT_OPERATOR_ID,
        ),
        req("Consultar Operadores (HU-23)", "GET", "api/v1/operators"),
        req(
            "Desactivar Operador (HU-26)",
            "PUT",
            "api/v1/operators/{{operatorId}}/deactivate",
        ),
        req(
            "6 · Asignar Operador (HU-24)",
            "POST",
            "api/v1/missions/{{missionId}}/operators",
            {"operatorId": "{{operatorId}}"},
        ),
        req(
            "Revocar Operador (HU-25)",
            "DELETE",
            "api/v1/missions/{{missionId}}/operators/{{operatorId}}",
        ),
    ]

    players = [
        req(
            "Crear Jugador",
            "POST",
            "api/v1/players",
            {
                "firstName": "Luis",
                "lastName": "Jugador",
                "email": "luis@umbral.com",
                "password": "SecurePass1",
            },
            test_script=SCRIPT_PLAYER_IDS,
        ),
        req("Consultar Jugadores", "GET", "api/v1/players"),
        req("Consultar Jugador por Id", "GET", "api/v1/players/{{playerId}}"),
        req(
            "Modificar Jugador",
            "PUT",
            "api/v1/players/{{playerId}}",
            {
                "firstName": "Luis",
                "lastName": "Actualizado",
                "email": "luis.nuevo@umbral.com",
            },
        ),
        req(
            "Desactivar Jugador",
            "PUT",
            "api/v1/players/{{playerId}}/deactivate",
        ),
    ]

    e4 = [
        req(
            "7 · Crear Equipo (HU-27)",
            "POST",
            "api/v1/teams",
            {
                "name": "Los Exploradores",
                "creatorId": "{{creatorId}}",
                "creatorDisplayName": "Líder",
            },
            service="session",
            test_script=SCRIPT_TEAM_ID,
        ),
        req(
            "Consultar Equipo (HU-29)",
            "GET",
            "api/v1/teams/{{teamId}}",
            service="session",
            test_script=SCRIPT_TEAM_CODE,
        ),
        req(
            "Modificar Equipo (HU-33)",
            "PUT",
            "api/v1/teams/{{teamId}}",
            {"newName": "Equipo Renombrado", "requestorId": "{{requestorId}}"},
            service="session",
        ),
        req(
            "Disolver Equipo (HU-34)",
            "DELETE",
            "api/v1/teams/{{teamId}}",
            query={"requestorId": "{{requestorId}}"},
            service="session",
        ),
        req(
            "Solicitar Unión (HU-28)",
            "POST",
            "api/v1/teams/join-requests",
            {
                "teamCode": "{{teamCode}}",
                "playerRef": "{{playerId}}",
                "displayName": "Nuevo miembro",
            },
            service="session",
            test_script=SCRIPT_REQUEST_ID,
        ),
        req(
            "Solicitudes Pendientes (HU-32)",
            "GET",
            "api/v1/teams/{{teamId}}/requests",
            query={"requestorId": "{{requestorId}}"},
            service="session",
            test_script=SCRIPT_FIRST_PENDING_REQUEST,
        ),
        req(
            "Procesar Solicitud (HU-30)",
            "PUT",
            "api/v1/teams/{{teamId}}/requests/{{requestId}}",
            {"approve": True, "requestorId": "{{requestorId}}"},
            service="session",
        ),
        req(
            "Expulsar Integrante (HU-31)",
            "DELETE",
            "api/v1/teams/{{teamId}}/members/{{playerId}}",
            query={"requestorId": "{{requestorId}}"},
            service="session",
        ),
    ]

    e5 = [
        req("Sesiones Activas (HU-36)", "GET", "api/v1/live-sessions/active", service="session"),
        req(
            "9 · Unirse a Sesión (HU-37)",
            "POST",
            "api/v1/live-sessions/join",
            {"joinCode": "{{joinCode}}", "teamId": "{{teamId}}"},
            service="session",
            test_script=SCRIPT_JOIN_SESSION,
        ),
        req(
            "Etapa Actual (HU-38)",
            "GET",
            "api/v1/live-sessions/{{sessionId}}/teams/{{teamId}}/current-stage",
            service="session",
        ),
        req(
            "Código Treasure Hunt (HU-40)",
            "POST",
            "api/v1/live-sessions/{{sessionId}}/teams/{{teamId}}/treasure-hunt-code",
            {"nodeId": "{{nodeId}}", "foundCode": "CODE-123"},
            service="session",
        ),
        req(
            "Respuesta Trivia (HU-42)",
            "POST",
            "api/v1/live-sessions/{{sessionId}}/teams/{{teamId}}/trivia-answer",
            {"nodeId": "{{nodeId}}", "answer": "Caracas"},
            service="session",
        ),
    ]

    e6 = [
        req(
            "Misiones Asignadas (HU-47)",
            "GET",
            "api/v1/operators/{{operatorId}}/missions",
            service="session",
        ),
        req(
            "Validar Sesiones Abiertas por Misión",
            "GET",
            "api/v1/missions/{{missionId}}/session-validation/has-open",
            service="session",
        ),
        req(
            "8 · Crear Sesión Live (HU-48)",
            "POST",
            "api/v1/operators/{{operatorId}}/sessions",
            {"missionId": "{{missionId}}"},
            service="session",
            test_script=SCRIPT_SESSION,
        ),
        req(
            "Equipos en Sesión Pending (HU-49)",
            "GET",
            "api/v1/operators/{{operatorId}}/sessions/{{sessionId}}/teams",
            service="session",
        ),
        req(
            "10 · Iniciar Sesión (HU-50)",
            "PUT",
            "api/v1/operators/{{operatorId}}/sessions/{{sessionId}}/start",
            service="session",
        ),
        req(
            "Finalizar Sesión",
            "PUT",
            "api/v1/operators/{{operatorId}}/sessions/{{sessionId}}/finalize",
            service="session",
        ),
        req(
            "Cancelar Sesión",
            "PUT",
            "api/v1/operators/{{operatorId}}/sessions/{{sessionId}}/cancel",
            service="session",
        ),
    ]

    internal = [
        req("Node Validations (integración)", "GET", "api/v1/missions/{{missionId}}/node-validations"),
        req(
            "Operador tiene sesiones activas",
            "GET",
            "api/v1/operators/{{operatorId}}/session-validation/has-active",
            service="session",
        ),
        req(
            "Operador supervisa misión",
            "GET",
            "api/v1/operators/{{operatorId}}/missions/{{missionId}}/session-validation/is-supervising",
            service="session",
        ),
    ]

    # Flujo E2E ordenado (Collection Runner de arriba a abajo)
    flujo_e2e = [
        req(
            "01 Crear Jugador",
            "POST",
            "api/v1/players",
            {
                "firstName": "Luis",
                "lastName": "Jugador",
                "email": "luis@umbral.com",
                "password": "SecurePass1",
            },
            test_script=SCRIPT_PLAYER_IDS,
        ),
        req(
            "02 Crear Operador",
            "POST",
            "api/v1/operators",
            {
                "firstName": "Ana",
                "lastName": "Operador",
                "email": "ana@umbral.com",
                "password": "SecurePass1",
            },
            test_script=SCRIPT_OPERATOR_ID,
        ),
        req(
            "03 Crear Misión",
            "POST",
            "api/v1/missions",
            {
                "title": "Misión E2E",
                "description": "Flujo automático",
                "difficulty": 1,
                "maxDurationMinutes": 60,
            },
            test_script=SCRIPT_MISSION_ID,
        ),
        req(
            "04 Crear Etapa",
            "POST",
            "api/v1/missions/{{missionId}}/nodes",
            {
                "title": "Etapa 1",
                "description": "Etapa",
                "executionOrder": 1,
            },
            test_script=SCRIPT_STAGE_ID,
        ),
        req(
            "05 Añadir Trivia",
            "POST",
            "api/v1/missions/{{missionId}}/nodes/{{parentNodeId}}/trivia",
            {
                "questions": [
                    {
                        "prompt": "¿Capital de Venezuela?",
                        "options": ["Caracas", "Valencia", "Maracaibo"],
                        "correctOptionIndex": 0,
                    }
                ],
                "executionOrder": 1,
            },
            test_script=SCRIPT_GAME_NODE_ID,
        ),
        req("06 Activar Misión", "PUT", "api/v1/missions/{{missionId}}/activate"),
        req(
            "07 Asignar Operador",
            "POST",
            "api/v1/missions/{{missionId}}/operators",
            {"operatorId": "{{operatorId}}"},
        ),
        req(
            "08 Crear Equipo",
            "POST",
            "api/v1/teams",
            {
                "name": "Equipo E2E",
                "creatorId": "{{creatorId}}",
                "creatorDisplayName": "Líder",
            },
            service="session",
            test_script=SCRIPT_TEAM_ID,
        ),
        req(
            "09 Crear Sesión Live",
            "POST",
            "api/v1/operators/{{operatorId}}/sessions",
            {"missionId": "{{missionId}}"},
            service="session",
            test_script=SCRIPT_SESSION,
        ),
        req(
            "10 Unir Equipo a Sesión",
            "POST",
            "api/v1/live-sessions/join",
            {"joinCode": "{{joinCode}}", "teamId": "{{teamId}}"},
            service="session",
            test_script=SCRIPT_JOIN_SESSION,
        ),
        req(
            "11 Iniciar Sesión",
            "PUT",
            "api/v1/operators/{{operatorId}}/sessions/{{sessionId}}/start",
            service="session",
        ),
    ]

    collection = {
        "info": {
            "_postman_id": uid(),
            "name": "UMBRAL API",
            "description": (
                "## Variables de colección\n"
                "Tras cada POST/PUT de creación, los scripts **Tests** guardan automáticamente "
                "`missionId`, `stageId`, `parentNodeId`, `nodeId`, `operatorId`, `playerId`, "
                "`teamId`, `sessionId`, `joinCode`, `teamCode`, `requestId`, `hintId`, etc.\n\n"
                "**URLs (Docker):** `{{missionManagementUrl}}` (5260) y `{{sessionManagementUrl}}` (5278).\n"
                "Si ejecutas con `dotnet run`: usa 5117 y 5240 en esas variables.\n\n"
                "**Flujo rápido:** carpeta «Flujo E2E» → Run collection (orden secuencial).\n\n"
                "Edita solo emails/contraseñas en los bodies de creación si hace falta."
            ),
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "event": [
            {
                "listen": "prerequest",
                "script": {
                    "type": "text/javascript",
                    "exec": [line for line in COLLECTION_PREREQUEST.strip().split("\n")],
                },
            }
        ],
        "variable": [
            {"key": "missionManagementUrl", "value": "http://localhost:5260", "type": "string"},
            {"key": "sessionManagementUrl", "value": "http://localhost:5278", "type": "string"},
            {"key": "missionManagementPort", "value": "5260", "type": "string"},
            {"key": "sessionManagementPort", "value": "5278", "type": "string"},
            {"key": "missionId", "value": "", "type": "string"},
            {"key": "nodeId", "value": "", "type": "string"},
            {"key": "stageId", "value": "", "type": "string"},
            {"key": "parentNodeId", "value": "", "type": "string"},
            {"key": "hintId", "value": "", "type": "string"},
            {"key": "operatorId", "value": "", "type": "string"},
            {"key": "playerId", "value": "", "type": "string"},
            {"key": "teamId", "value": "", "type": "string"},
            {"key": "teamCode", "value": "", "type": "string"},
            {"key": "requestId", "value": "", "type": "string"},
            {"key": "sessionId", "value": "", "type": "string"},
            {"key": "joinCode", "value": "", "type": "string"},
            {"key": "requestorId", "value": "", "type": "string"},
            {"key": "creatorId", "value": "", "type": "string"},
            {"key": "lastCreatedId", "value": "", "type": "string"},
        ],
        "item": [
            folder(
                "Flujo E2E (Run Collection)",
                flujo_e2e,
                "Ejecuta esta carpeta con Collection Runner. Las variables se rellenan solas entre pasos.",
            ),
            folder(
                "MissionManagement",
                [
                    folder("Épica 1 — Misiones y Etapas", e1),
                    folder("Épica 2 — Juegos y Pistas", e2),
                    folder("Épica 3 — Operadores", e3),
                    folder("Gestión de Jugadores", players),
                ],
            ),
            folder(
                "SessionManagement",
                [
                    folder("Épica 4 — Equipos", e4),
                    folder("Épica 5 — Participación Jugador", e5),
                    folder("Épica 6 — Operador", e6),
                ],
            ),
            folder("Integración (solo en código)", internal),
        ],
    }

    out = Path(__file__).resolve().parent.parent / "Umbral-Postman-Collection.json"
    out.write_text(json.dumps(collection, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Written: {out}")


if __name__ == "__main__":
    main()
