# UMBRAL · Backend Spec: Épica 3 (Gestión y Asignación de Operadores)

## Contexto
Este spec define la implementación técnica para la creación de cuentas de Operador, su habilitación/inhabilitación y la asignación de permisos sobre misiones específicas.
- **Bounded Context:** Admin Service (Gestión de Catálogo y Accesos) interactuando con IAM (Keycloak).
- **Actor Principal:** Administrador.
- **Historias de Usuario cubiertas:** HU-22 a HU-26.

## Entidades Involucradas
- `Operador` (Entidad / Representación del usuario en el dominio)
- `Mision` (Agregado Raíz - Referencia cruzada)

## Reglas de Negocio y Validaciones (Business Rules)
- [cite_start]**RN-16 (Restricción de Acceso Operativo):** Un Operador únicamente puede operar sobre misiones que le hayan sido explícitamente asignadas[cite: 44].
- [cite_start]**Control de Unicidad (HU-22 & HU-24):** No se pueden registrar correos duplicados para cuentas nuevas, ni se puede asignar un operador a la misma misión dos veces[cite: 13].
- [cite_start]**Bloqueo de Revocación (HU-25):** No se puede revocar a un operador de una misión si actualmente está supervisando una sesión en vivo de la misma[cite: 13].
- [cite_start]**Bloqueo de Desactivación (HU-26):** No se puede desactivar a un operador a nivel global si este tiene cualquier sesión activa en ejecución[cite: 13].

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Cuentas de Operador (HU-22, HU-23, HU-26)
**Ruta Base:** `api/v1/operators`

- **HU-22: Crear cuenta de Operador**
  - **Endpoint:** `POST /api/v1/operators`
  - **Command:** `CreateOperatorCommand(string Nombre, string Correo)`
  - **Flujo Técnico:** El Handler debe registrar al usuario en Keycloak (asociándole el rol `operator`), generar su ID y guardarlo en la base de datos del Admin Service.
  - [cite_start]**Validación:** Rechazar si el correo electrónico ya existe[cite: 13].

- **HU-23: Consultar Operadores**
  - **Endpoint:** `GET /api/v1/operators`
  - **Query:** `GetOperatorsQuery`
  - **DTO de salida:** Listado de operadores y sus estados (Activo/Inactivo). [cite_start]Si no hay registros, devolver arreglo vacío[cite: 13].

- **HU-26: Desactivar Operador**
  - **Endpoint:** `PUT /api/v1/operators/{operatorId}/deactivate`
  - **Command:** `DeactivateOperatorCommand(Guid OperatorId)`
  - **Validación:** Se debe consultar de forma síncrona o eventual al `Session Management` para verificar si tiene sesiones activas. [cite_start]Si las tiene, bloquear la desactivación[cite: 13].

### 2. Asignación de Operadores a Misiones (HU-24, HU-25)
**Ruta Base:** `api/v1/missions/{missionId}/operators`

- **HU-24: Asignar Operador a Misión**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/operators`
  - **Command:** `AssignOperatorToMissionCommand(Guid MissionId, Guid OperatorId)`
  - **Validación:** Verificar que el operador no esté previamente asignado a esa misión. [cite_start]Alertar conflicto si ya existe[cite: 13].

- **HU-25: Revocar Operador de Misión**
  - **Endpoint:** `DELETE /api/v1/missions/{missionId}/operators/{operatorId}`
  - **Command:** `RevokeOperatorFromMissionCommand(Guid MissionId, Guid OperatorId)`
  - [cite_start]**Validación:** El sistema debe verificar si el operador está actualmente supervisando una sesión en vivo de esa misión específica y bloquear la revocación pidiendo que finalice la sesión primero[cite: 13].