# REGLAS DE FRONTEND: UMBRAL (WEB ADMIN/OPERADOR)

## STACK TECNOLÓGICO
- Framework: React (Next.js o Vite).
- Estilos: Tailwind CSS + Shadcn UI (o componentes similares).
- Estado/Fetch: React Query (TanStack Query) + Zustand (si es necesario).
- Tiempo Real: Cliente de @microsoft/signalr.

## REGLAS DE ARQUITECTURA VISUAL Y CÓDIGO
1. **Arquitectura de Componentes:** Separa componentes lógicos (Smart/Container) de componentes visuales (Dumb/Presentational).
2. **Gestión del Tiempo Real:** El Dashboard del Operador debe ser altamente reactivo. Centraliza la conexión de SignalR en un Hook personalizado (ej. `useGameSession()`) que escuche los eventos de RabbitMQ procesados por el backend.