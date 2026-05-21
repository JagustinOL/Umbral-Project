namespace AdminService.Domain.ValueObjects;

public record DifficultyLevel
{
    public string Value { get; init; }

    private DifficultyLevel(string value) => Value = value;

    public static DifficultyLevel Easy => new("Fácil");
    public static DifficultyLevel Medium => new("Media");
    public static DifficultyLevel Hard => new("Difícil");

    public static DifficultyLevel Create(string value)
    {
        string[] validLevels = ["Fácil", "Media", "Difícil"];
        if (!validLevels.Contains(value))
            throw new ArgumentException("Nivel de dificultad inválido.");
            
        return new DifficultyLevel(value);
    }
}