# CQRS MediatR Skill

## Patrón de Command Handler
public class CreateMissionCommand : IRequest<Guid> 
{
    public string Name { get; init; }
    public Difficulty Level { get; init; }
}

public class CreateMissionHandler : IRequestHandler<..., Guid> 
{
    // constructor con IRepository
    public async Task<Guid> Handle(...) { ... }
}

## Anti-patrones a evitar
- No inyectar DbContext directo