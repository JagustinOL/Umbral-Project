# UMBRAL · Backend Spec: Épica 3 (Gestión y Asignación de Operadores)

## Contexto
Este spec define la implementación técnica para la creación de cuentas de Operador, su habilitación/inhabilitación y la asignación de permisos sobre misiones específicas.
- **Bounded Context:** Identity & Access Management (Keycloak) y Mission Management.
- **Actor Principal:** Administrador (Autenticado vía Keycloak).
- **Historias de Usuario cubiertas:** HU-22 a HU-26.

## Entidades y Value Objects Involucrados
- `OperatorRef` (Value Object / Referencia al ID único generado por Keycloak)
- `Mission` (Agregado Raíz - Contiene la lista interna de operadores asignados)

## Reglas de Negocio y Validaciones (Business Rules)
- **RN-16 (Restricción de Acceso Operativo):** Un Operador únicamente puede operar sobre misiones que le hayan sido explícitamente asignadas. El dominio de `Mission` protege esto validando su lista interna de `OperatorRef`.
- **Control de Unicidad (HU-22 & HU-24):** No se pueden registrar correos duplicados para cuentas nuevas (Keycloak bloquea esto). Tampoco se puede asignar un operador a la misma misión dos veces (El Agregado `Mission` lanza excepción).
- **Bloqueo de Revocación (HU-25):** No se puede revocar a un operador de una misión si actualmente está supervisando una sesión en vivo de la misma (Requiere validación cruzada con `SessionManagement`).
- **Bloqueo de Desactivación (HU-26):** No se puede desactivar a un operador a nivel global si este tiene cualquier sesión activa en ejecución en todo el sistema.

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Cuentas de Operador (HU-22, HU-23, HU-26)
**Ruta Base:** `api/v1/operators` *(Gateway o Controlador que actúa como fachada de Keycloak)*

- **HU-22: Crear cuenta de Operador**
  - **Endpoint:** `POST /api/v1/operators`
  - **Command:** `CreateOperatorCommand(string FirstName, string LastName, string Email)`
  - **Flujo Técnico:** El Application Handler interactúa con la Admin API de Keycloak para registrar al usuario y asignarle el rol `operator`. Keycloak genera el `Guid` (OperatorId).
  - **Validación:** Keycloak rechazará automáticamente la transacción si el correo electrónico ya existe.

- **HU-23: Consultar Operadores**
  - **Endpoint:** `GET /api/v1/operators`
  - **Query:** `GetOperatorsQuery`
  - **DTO de salida:** Listado de operadores y sus estados (Activo/Inactivo) obtenidos desde Keycloak. Si no hay registros, devolver arreglo vacío.

- **HU-26: Desactivar Operador**
  - **Endpoint:** `PUT /api/v1/operators/{operatorId}/deactivate`
  - **Command:** `DeactivateOperatorCommand(Guid OperatorId)`
  - **Validación:** El Handler debe consultar de forma síncrona a la API de `SessionManagement` para verificar si el operador tiene sesiones activas. Si las tiene, lanzar excepción y bloquear la desactivación en Keycloak.

### 2. Asignación de Operadores a Misiones (HU-24, HU-25)
**Ruta Base:** `api/v1/missions/{missionId}/operators` *(Dentro de Mission Management)*

- **HU-24: Asignar Operador a Misión**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/operators`
  - **Command:** `AssignOperatorToMissionCommand(Guid MissionId, Guid OperatorId)`
  - **Validación:** El comando delega la acción a `mission.AssignOperator(OperatorId)`. El Agregado valida internamente que el operador no esté previamente asignado (Alertar conflicto de unicidad).

- **HU-25: Revocar Operador de Misión**
  - **Endpoint:** `DELETE /api/v1/missions/{missionId}/operators/{operatorId}`
  - **Command:** `RevokeOperatorFromMissionCommand(Guid MissionId, Guid OperatorId)`
  - **Validación:** El Handler primero realiza una consulta a `SessionManagement` para verificar si el operador está actualmente supervisando una sesión en vivo (LiveSession) de esa misión específica. Si es así, bloquea. Si está libre, invoca `mission.RevokeOperator(OperatorId)` y persiste el cambio.