# RabbitMQ Events Skill

## Eventos de Integración
- Nomenclatura: Los eventos deben nombrarse en pasado (Ej. `SessionStartedEvent`, `TeamRegisteredEvent`).
- Ubicación: Los contratos de eventos compartidos deben estar en un paquete NuGet común o en una carpeta compartida/abstracción.

## Publicación y Consumo (MassTransit / RabbitMQ)
- Publicar eventos desde los Handlers de MediatR después de que la transacción de la base de datos sea exitosa.
- Implementar `IConsumer<TEvent>` para reaccionar a eventos de otros microservicios.

## Anti-patrones a evitar
- Enviar objetos del dominio completos en el payload del evento. Enviar solo los IDs y datos esenciales (Primitive Data).