# EF Core & PostgreSQL Skill

## Configuraciones (Fluent API)
- NO usar Data Annotations (como `[Table]`, `[Key]`, `[Required]`) en las clases de Dominio.
- Toda la configuración de mapeo debe hacerse en la capa de `Infrastructure` usando clases que implementen `IEntityTypeConfiguration<T>`.

## Repositorios
- Implementar las interfaces definidas en la capa de Dominio (ej. `ISessionRepository`).
- Ocultar la complejidad de `DbContext` detrás del repositorio.

## Anti-patrones a evitar
- Exponer `IQueryable<T>` fuera de la capa de Infraestructura. Retornar siempre `Task<IEnumerable<T>>` o paginaciones concretas.