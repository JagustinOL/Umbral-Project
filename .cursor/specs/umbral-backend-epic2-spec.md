# UMBRAL · Backend Spec: Épica 2 (Juegos y Pistas)

## Contexto
[cite_start]Este spec define la implementación técnica para la configuración de los retos específicos (Juegos) que conforman las etapas de una misión, así como los recursos de ayuda (Pistas).
- **Bounded Context:** Admin Service.
- **Actor Principal:** Administrador.
- [cite_start]**Historias de Usuario cubiertas:** HU-09 a HU-21[cite: 9, 10, 11].

## Entidades y Value Objects Involucrados
- `Juego` (Clase Abstracta / Entidad Base)
- `JuegoTrivia` (Entidad Derivada)
- `JuegoBusqueda` (Entidad Derivada)
- `Pista` (Entidad)
- `PreguntaTrivia` (Value Object)
- `CoordenadaGPS` (Value Object)

## Reglas de Negocio (Business Rules)
- [cite_start]**RN-01 (Inmutabilidad en Uso):** No se puede editar ni eliminar un Juego o Pista si existe al menos una Sesión activa (en curso o pausada) vinculada a su misión padre[cite: 32].
- [cite_start]**RN-02 (Tipificación Obligatoria):** El sistema solo permite dos tipos de juego: Trivia y Búsqueda del Tesoro[cite: 33].

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Juegos de Trivia (HU-09 a HU-12)
**Ruta Base:** `api/v1/stages/{stageId}/trivia`

- **HU-09: Añadir Trivia**
  - **Endpoint:** `POST /api/v1/stages/{stageId}/trivia`
  - **Command:** `AddTriviaCommand(Guid StageId, List<PreguntaTrivia> Preguntas)`
  - [cite_start]**Validación:** El sistema rechaza la transacción si no se marca ninguna respuesta como "Correcta"[cite: 9]. Ejecutar control RN-01.

- **HU-10: Consultar Trivia**
  - **Endpoint:** `GET /api/v1/stages/{stageId}/trivia`
  - **Query:** `GetTriviaByStageQuery(Guid StageId)`
  - [cite_start]**DTO de salida:** Muestra las preguntas y respuestas configuradas[cite: 9].

- **HU-11: Modificar Trivia**
  - **Endpoint:** `PUT /api/v1/stages/{stageId}/trivia/{triviaId}`
  - **Command:** `UpdateTriviaCommand(...)`
  - [cite_start]**Validación:** Bloquear si se intenta eliminar la única respuesta correcta de una pregunta[cite: 9]. Ejecutar control RN-01.

- **HU-12: Eliminar Trivia**
  - **Endpoint:** `DELETE /api/v1/stages/{stageId}/trivia/{triviaId}`
  - **Command:** `DeleteTriviaCommand(Guid StageId, Guid TriviaId)`
  - [cite_start]**Validación:** Ejecutar control RN-01[cite: 9].

### 2. Gestión de Juegos de Búsqueda (HU-13 a HU-16)
**Ruta Base:** `api/v1/stages/{stageId}/treasure-hunt`

- **HU-13: Añadir Búsqueda**
  - **Endpoint:** `POST /api/v1/stages/{stageId}/treasure-hunt`
  - **Command:** `AddTreasureHuntCommand(Guid StageId, string Instrucciones, string CodigoEncuentro, CoordenadaGPS Destino)`
  - [cite_start]**Validación:** El sistema exige datos clave (como mapa/coordenadas) antes de guardar[cite: 10]. Ejecutar control RN-01.

- **HU-14 a HU-16: Consultar, Modificar y Eliminar Búsqueda**
  - **Endpoints:** `GET`, `PUT`, `DELETE` en la ruta base con `{huntId}`.
  - [cite_start]**Validación:** Las modificaciones e intentos de eliminación deben bloquearse si la misión está activa (RN-01)[cite: 10].

### 3. Gestión de Pistas (HU-17 a HU-21)
**Ruta Base:** `api/v1/treasure-hunts/{huntId}/clues`

- **HU-17: Crear Pista**
  - **Endpoint:** `POST /api/v1/treasure-hunts/{huntId}/clues`
  - **Command:** `AddClueCommand(Guid HuntId, string Texto, IFormFile ArchivoAdjunto)`
  - [cite_start]**Validación:** Rechazar si el contenido multimedia supera el tamaño máximo o si el formato no es válido (solo JPG/PNG permitidos)[cite: 10].

- **HU-18 a HU-20: Consultar, Modificar y Eliminar Pista**
  - **Endpoints:** `GET`, `PUT`, `DELETE` en la ruta base con `{clueId}`.
  - [cite_start]**Validación:** No se pueden alterar pistas de misiones activas (RN-01)[cite: 10].

- **HU-21: Configuración de Liberación**
  - [cite_start]**Regla del Dominio:** Asegurar a nivel de dominio que la estructura relacional permita que las pistas se entreguen al equipo al terminar una búsqueda[cite: 11].