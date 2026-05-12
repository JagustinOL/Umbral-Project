# REGLAS DE MOBILE: UMBRAL (APP JUGADORES/EQUIPOS)

## STACK TECNOLÓGICO
- Framework: React Native (Expo) o equivalente.
- Estilos: Tailwind (NativeWind) o StyleSheet optimizado.
- Tiempo Real: Cliente SignalR adaptado para Mobile.
- Hardware: Librería de cámara para escaneo de Códigos QR.

## REGLAS DE EXPERIENCIA Y CÓDIGO
1. **Prioridad Inmersiva:** La UI debe parecer un "dispositivo de investigación". Oscuro, con retroalimentación háptica (vibración) cuando llegue una pista o se reciba una penalización.
2. **Lector QR (Búsqueda del Tesoro):** El componente de cámara debe ser rápido. Al escanear un código, debe bloquear escaneos subsecuentes hasta que el backend valide el código a través de la API (prevención de spam).
3. **Resiliencia de Red:** Los recintos de Escape Room pueden tener mala señal. Implementa reintentos automáticos para la reconexión del WebSocket (SignalR). Si la red se cae, muestra un indicador claro de "Buscando señal..." en lugar de congelar la pantalla.
4. **Estado de Etapa:** La pantalla principal siempre debe reflejar la `Etapa` actual dictada por el backend. No calcules lógicas de "si ganaron o no" en el móvil, confía 100% en los eventos que envía el `GameEngineService`.