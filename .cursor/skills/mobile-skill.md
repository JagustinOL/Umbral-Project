# Mobile Skill (App Jugadores/Equipos)

## Stack tecnológico
- Framework: React Native (Expo) o equivalente.
- Estilos: Tailwind (NativeWind) o StyleSheet optimizado.
- Tiempo real: Cliente SignalR adaptado para mobile.
- Hardware: Librería de cámara para escaneo de códigos QR.

## Reglas de experiencia y código
1. **Prioridad inmersiva:** La UI debe parecer un "dispositivo de investigación". Oscuro, con retroalimentación háptica (vibración) cuando llegue una pista o se reciba una penalización.
2. **Lector QR (búsqueda del tesoro):** El componente de cámara debe ser rápido. Al escanear un código, debe bloquear escaneos subsecuentes hasta que el backend valide el código a través de la API (prevención de spam).
3. **Resiliencia de red:** Los recintos de escape room pueden tener mala señal. Implementa reintentos automáticos para la reconexión del WebSocket (SignalR). Si la red se cae, muestra un indicador claro de "Buscando señal..." en lugar de congelar la pantalla.
4. **Estado de etapa:** La pantalla principal siempre debe reflejar la `Etapa` actual dictada por el backend. No calcules lógicas de "si ganaron o no" en el móvil; confía 100% en los eventos que envía el `GameEngineService`.

## Anti-patrones a evitar
- Duplicar reglas de negocio del backend en el cliente.
- Mantener estado de partida local que contradiga eventos del servidor.
