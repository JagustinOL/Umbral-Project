# DDD Modeling Skill

## Reglas de Entidades y Agregados (Aggregate Roots)
- Los constructores deben ser privados o protegidos sin parámetros para ORMs, y públicos/internal ricos en parámetros para creación.
- Las propiedades deben tener `private set` o `init`. El estado solo se muta a través de métodos públicos con nombres que expresen la intención (ej. `StartSession()`, no `Status = Active`).

## Value Objects (Objetos de Valor)
- Implementarlos SIEMPRE usando la palabra reservada `record` de C# 9+ para garantizar inmutabilidad.
- Validar las reglas de integridad en el constructor del `record`.

## Anti-patrones a evitar
- **Modelo Anémico:** Clases que solo tienen getters/setters públicos sin comportamiento.
- Dependencias de infraestructura (EF Core, JSON, HTTP) dentro de la carpeta `Domain`.