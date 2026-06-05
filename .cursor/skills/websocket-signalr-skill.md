# WebSocket SignalR Skill

## Diseño de Hubs
- Crear Hubs fuertemente tipados heredando de `Hub<IMyClientContract>`.
- Agrupar conexiones por sesión (Ej. `await Groups.AddToGroupAsync(Context.ConnectionId, "Session_123");`).

## Responsabilidades
- El Hub NO debe contener lógica de negocio. Debe invocar a MediatR o a servicios de aplicación para procesar acciones.
- Usar SignalR principalmente para broadcasting (hacia el cliente) de actualizaciones de estado (Ej. `ReceiveScoreUpdate`).

## Anti-patrones a evitar
- Mantener estado en memoria en la clase del Hub (los Hubs son transitorios).