# REGLAS DE BACKEND: UMBRAL (MICROSERVICIOS .NET)

## STACK TECNOLÓGICO
- Lenguaje: C# 12 / .NET 8.
- Patrones: Arquitectura Hexagonal / Clean Architecture, DDD, CQRS.
- Librerías clave: MediatR (para CQRS), Entity Framework Core (PostgreSQL), SignalR (WebSockets), MassTransit/RabbitMQ (Eventos).

## MICROSERVICIOS DEFINIDOS
1. `CatalogService`: CRUD de Misiones y Juegos.
2. `IdentityService`: Autenticación y JWT.
3. `TeamService`: Gestión de Equipos e Invitaciones.
4. `GameEngineService`: Motor en vivo (WebSockets).
5. `AnalyticsService`: Ranking y Trazabilidad (Consumidor RabbitMQ).

## REGLAS ESTRICTAS DE CÓDIGO (DDD & CQRS)
1. **Mutación por Comportamiento:** Todo cambio de estado se hace mediante métodos de dominio con lenguaje ubicuo (ej. `public void RecibirPenalizacion(string motivo, int valor)`).
2. **Value Objects:** Usa `record` para conceptos inmutables que no tienen identidad (ej. `Puntaje`, `CodigoUnion`).
3. **Eventos de Dominio:** Cualquier cambio crítico en `GameEngineService` debe publicar un evento hacia RabbitMQ (ej. `JuegoCompletadoEvent`) para no bloquear el hilo principal de la sesión.