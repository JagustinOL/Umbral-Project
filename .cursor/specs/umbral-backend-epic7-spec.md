# UMBRAL · Backend Spec: Épica 7 (Trazabilidad y Auditoría)

## Contexto
Este spec define la implementación técnica para la consulta histórica de sesiones finalizadas. Su objetivo es proveer a los actores autorizados un registro inmutable de todo lo ocurrido durante la experiencia.
- **Bounded Context:** Scoring & Audit Context (Microservicio independiente de solo lectura/analítica).
- **Actores Principales:** Administrador y Operador (Autenticados vía Keycloak).
- **Historias de Usuario cubiertas:** HU-64 y HU-65.

## Entidades y Proyecciones Involucrados
- `AuditLog` (Agregado Raíz / Proyección de solo lectura en base de datos)
- `TeamLedger` (Libro Mayor inmutable que contiene los puntos del equipo)
- `ScoreEntry` (Entidad hija que registra cada variación de puntos)
- `HistoricalSessionDto` (DTO optimizado de solo lectura)

## Reglas de Negocio y Validaciones (Business Rules)
- **RN-17 (Inmutabilidad Post-Cierre):** Una vez que el Operador o el sistema marca una sesión como Finalizada en el *Live Engine*, el *ScoringAudit* recibe el evento y sella los registros. Ningún puntaje, tiempo, entrada de auditoría o estado puede ser alterado. El registro pasa a ser estrictamente de solo lectura.
- **Seguridad de Acceso:** Los operadores solo pueden auditar los registros históricos de las sesiones pertenecientes a las misiones que tienen explícitamente asignadas. Los administradores tienen acceso global.
- **Cero Mutaciones Externas:** La capa API de este microservicio carece por completo de endpoints `POST`, `PUT` o `DELETE`. Toda la escritura en la base de datos se hace mediante **Event Handlers** que consumen mensajes de RabbitMQ (ej. `SessionEventOccurred`, `ScoreUpdated`).

## Estructura CQRS y Endpoints (API REST)

### 1. Consulta de Auditoría e Historial (HU-64 y HU-65)
Al ser un módulo de trazabilidad, la arquitectura se enfoca únicamente en `Queries` de alto rendimiento.

**Ruta Base:** `api/v1/audit/sessions`

- **HU-64: Consultar Historial de Sesiones Finalizadas**
  - **Endpoint:** `GET /api/v1/audit/sessions`
  - **Query:** `GetHistoricalSessionsQuery(Guid? OperatorId, DateTime? StartDate, DateTime? EndDate)`
  - **Flujo Técnico:** El sistema devuelve una lista paginada de las sesiones en estado `Finished`. Si no hay registros de auditoría disponibles para los filtros aplicados, el sistema muestra un estado vacío (Empty State).
  - **Validación:** El Handler debe inyectar el contexto de seguridad para asegurar que el `OperatorId` coincida con el usuario autenticado en el token JWT (salvo que tenga rol de Administrador).

- **HU-65: Ver Detalle de Auditoría por Sesión**
  - **Endpoint:** `GET /api/v1/audit/sessions/{sessionId}`
  - **Query:** `GetSessionAuditDetailQuery(Guid SessionId)`
  - **DTO de salida:** Debe incluir una línea de tiempo inmutable con:
    - Tiempos exactos de inicio y fin.
    - Evidencias enviadas por cada equipo con sus respectivas respuestas de validación.
    - Penalizaciones manuales aplicadas (cruzando los datos del `ScoreEntry` con el motivo y el operador responsable).
    - Ranking y puntaje final calculado a partir de la consolidación de los `TeamLedger`.
  - **Validación:** Rechazar la petición devolviendo un 404 (Not Found) o 400 (Bad Request) si la sesión consultada no se encuentra registrada o no ha finalizado.