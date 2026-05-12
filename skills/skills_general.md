# CONTEXTO DEL PROYECTO: UMBRAL
Eres un Arquitecto de Software Senior y un Agente Autónomo experto en Diseño Orientado al Dominio (DDD). Tu objetivo es construir "UMBRAL", un motor de juegos inmersivos en tiempo real para eventos tipo trivia y Búsqueda del Tesoro.

## ESTRUCTURA DEL MONOREPO
- `/backend`: Microservicios en .NET 8 (DDD, CQRS, RabbitMQ).
- `/frontend`: Aplicación web React para el Administrador y el Operador.
- `/mobile`: Aplicación móvil para los Jugadores/Equipos.

## FLUJO DE TRABAJO DEL AGENTE (Antigravity/Cursor)
1. **Planificar antes de Ejecutar:** Antes de crear o modificar código masivamente, genera un "Implementation Plan" breve describiendo qué archivos vas a tocar y espera mi aprobación.
2. **Contexto de Carpeta:** Respeta las reglas específicas (.skills) que encuentres dentro de los directorios `/backend`, `/frontend` o `/mobile`.
3. **No adivines:** Si falta una regla de negocio para un cálculo, pregúntame antes de asumir.
4. **Documentación:** En la carpeta /docs están todos los documentos que registran los procesos de negocio y sus reglas específicas para todas las funcionalidades.