# UMBRAL · Backend Spec: Épica 7 (Trazabilidad y Auditoría)

## Contexto
Este spec define la implementación técnica para la consulta histórica de sesiones finalizadas. Su objetivo es proveer a los actores autorizados un registro inmutable de todo lo ocurrido durante la experiencia.
- **Bounded Context:** Session Management (Read-Only / Analytics).
- **Actores Principales:** Administrador y Operador.
- **Historias de Usuario cubiertas:** HU-64 y HU-65.

## Entidades y Proyecciones Involucradas
- `RegistroAuditoria` (Read Model / Proyección)
- `SesionHistorica` (DTO de solo lectura)
- `Sesion` (Agregado Raíz - Referencia cruzada)

## Reglas de Negocio y Validaciones (Business Rules)
- [cite_start]**RN-17 (Inmutabilidad Post-Cierre):** Una vez que el Operador o el sistema marca una sesión como Finalizada, ningún puntaje, tiempo o estado puede ser alterado[cite: 46]. [cite_start]El registro pasa a ser estrictamente de solo lectura para la Auditoría[cite: 47].
- **Seguridad de Acceso:** Los operadores solo pueden auditar los registros históricos de las sesiones pertenecientes a las misiones que tienen explícitamente asignadas. Los administradores tienen acceso global.

## Estructura CQRS y Endpoints (API REST)

### 1. Consulta de Auditoría e Historial (HU-64 y HU-65)
Al ser un módulo de trazabilidad, la arquitectura debe prescindir de `Commands` y enfocarse únicamente en `Queries` de alto rendimiento.

**Ruta Base:** `api/v1/audit/sessions`

- **HU-64: Consultar Historial de Sesiones Finalizadas**
  - **Endpoint:** `GET /api/v1/audit/sessions`
  - **Query:** `GetHistoricalSessionsQuery(Guid? OperatorId, DateTime? FechaDesde, DateTime? FechaHasta)`
  - **Flujo Técnico:** El sistema devuelve una lista paginada de las sesiones en estado *Finalizada*. [cite_start]Si no hay registros de auditoría disponibles para los filtros aplicados, el sistema muestra un estado vacío (Empty State)[cite: 31].
  - **Validación:** El Handler debe inyectar el contexto de seguridad para asegurar que el `OperatorId` coincida con el usuario autenticado (salvo que sea Administrador).

- **HU-65: Ver Detalle de Auditoría por Sesión**
  - **Endpoint:** `GET /api/v1/audit/sessions/{sessionId}`
  - **Query:** `GetSessionAuditDetailQuery(Guid SessionId)`
  - **DTO de salida:** Debe incluir una línea de tiempo inmutable con:
    - Tiempos exactos de inicio y fin.
    - Evidencias enviadas por cada equipo con sus respectivas respuestas de validación.
    - Penalizaciones manuales aplicadas (con el motivo y el operador responsable).
    - Ranking y puntaje final calculado.
  - **Validación:** Rechazar la petición si la sesión consultada no se encuentra en estado *Finalizada*.