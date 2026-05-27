# Testing Skill

## Frameworks
- Usar **xUnit** como framework principal.
- Usar **Moq** o **NSubstitute** para simular dependencias (repositorios).
- Usar **FluentAssertions** para aserciones más legibles.

## Estructura de la Prueba (AAA)
- Cada prueba debe dividirse claramente con comentarios: `// Arrange`, `// Act`, `// Assert`.
- Nomenclatura: `MetodoAProbar_EstadoBajoPrueba_ComportamientoEsperado` (Ej. `StartSession_WithoutTeams_ThrowsDomainException`).

## Foco de Pruebas
- Priorizar Unit Tests puros en la capa de `Domain` (Entidades y Value Objects).
- Testear los Handlers de `Application` inyectando mocks de repositorios.