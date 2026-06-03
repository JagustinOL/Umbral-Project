# UMBRAL · Coding Standards

## 1. Legibilidad y Diseño (Clean Code & Bajo Acoplamiento)
- **Código que Expresa Intención:** El código debe leerse como un libro. Usa nombres descriptivos en lugar de comentarios (Ej. `bool isSessionActive` en lugar de `bool flag // indica si está activa`).
- **Early Returns (Cláusulas de Guarda):** Evita el anidamiento profundo (los `if` dentro de `if`). Valida los errores al principio de la función y retorna temprano para mantener el "camino feliz" plano.
- **Principio de Responsabilidad Única (SRP):** Una clase/componente debe hacer una sola cosa. Si un componente de React tiene más de 200 líneas o un Handler en C# hace validaciones, llamadas a base de datos y envía correos, divídelo.
- **Inyección de Dependencias (DI):** Nunca instancies clases complejas con `new` dentro de otra clase (acoplamiento fuerte). Todo servicio, repositorio o estrategia debe inyectarse por constructor.

## 2. Estándares Backend (C# .NET 9)
- **Clases, Records, Structs y Propiedades:** `PascalCase` (Ej: `LiveSession`, `TotalScore`).
- **Interfaces:** `PascalCase` pero siempre con el prefijo 'I' mayúscula (Ej: `ISessionRepository`).
- **Métodos:** `PascalCase` y deben ser verbos de acción (Ej: `CalculatePoints()`, `StartSession()`).
- **Variables Locales y Parámetros:** `camelCase` (Ej: `evidenceId`, `currentScore`).
- **Campos Privados (Private Fields):** `_camelCase` con guion bajo (Ej: `_dbContext`, `_sessionState`).
- **Estructura de Archivos:** Un archivo = una clase. El archivo debe llamarse exactamente igual que la clase (Ej: `TeamLeader.cs`).

## 3. Estándares Frontend Web (React / Next.js)
- **Componentes de React:** Nombres de funciones y archivos en `PascalCase` (Ej: `ScoreBoard.tsx`, `MissionDashboard.tsx`).
- **Hooks Personalizados:** `camelCase`, SIEMPRE comenzando con la palabra "use" (Ej: `useSessionManager.ts`, `useAuth()`).
- **Next.js App Router:** Las carpetas de enrutamiento van en `kebab-case` (Ej: `/app/mission-dashboard`). Los archivos reservados de Next.js van en minúsculas (Ej: `page.tsx`, `layout.tsx`).
- **Interfaces / Tipos (TypeScript):** `PascalCase`. NUNCA usar el prefijo 'I' en TypeScript (Ej: `User`, `SessionState` - no `IUser`).
- **Gestión de Estado:** Preferir el estado local. Extraer a estado global (Context/Zustand) solo si la información se comparte entre múltiples pantallas (Ej: el usuario logueado o el token JWT).

## 4. Estándares Frontend Mobile (React Native / Expo)
- **Componentes Visuales:** Igual que React Web (`PascalCase`).
- **Estilos (StyleSheet):** Los nombres de los objetos de estilo deben usar `camelCase` de forma semántica (Ej: `styles.container`, `styles.submitButtonText`).
- **Componentes Core:** Priorizar siempre los componentes nativos optimizados (`Pressable` sobre `TouchableOpacity`, o usar librerías probadas si el ecosistema lo exige).
- **Separación Lógica/Vista:** Evitar escribir funciones complejas directamente dentro del JSX. Extraer cálculos matemáticos o transformaciones de datos fuera del componente o en utilidades.