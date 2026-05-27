namespace MissionManagement.Domain.ValueObjects;

/// <summary>
/// Value Object que representa el nivel de dificultad de una Misión.
/// Como Value Object, es inmutable: dos DifficultyLevel con el mismo
/// nombre y multiplicador son idénticos por valor, no por referencia.
///
/// El multiplicador de puntaje es utilizado por el ScoreCalculatorService
/// del contexto ScoringAudit para aplicar la estrategia de cálculo correcta.
/// </summary>
public sealed record DifficultyLevel
{
    public static readonly DifficultyLevel Easy   = new("Easy",   multiplier: 1.0m);
    public static readonly DifficultyLevel Medium = new("Medium", multiplier: 1.5m);
    public static readonly DifficultyLevel Hard   = new("Hard",   multiplier: 2.0m);

    public string Name { get; }

    /// <summary>
    /// Factor por el que se multiplica el puntaje base al completar un nodo.
    /// Viaja como parte del evento EvidenceValidated para que ScoringAudit
    /// aplique la estrategia correcta sin consultar a este contexto.
    /// </summary>
    public decimal ScoreMultiplier { get; }

    private DifficultyLevel(string name, decimal multiplier)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de dificultad no puede estar vacío.", nameof(name));
        if (multiplier <= 0)
            throw new ArgumentException("El multiplicador debe ser mayor que cero.", nameof(multiplier));

        Name = name;
        ScoreMultiplier = multiplier;
    }

    /// <summary>
    /// Parsea un nombre de dificultad desde persistencia o API.
    /// Lanza excepción si el valor no es reconocido.
    /// </summary>
    public static DifficultyLevel FromName(string name) => name switch
    {
        "Easy"   => Easy,
        "Medium" => Medium,
        "Hard"   => Hard,
        _ => throw new ArgumentOutOfRangeException(nameof(name),
                 $"Nivel de dificultad desconocido: '{name}'. Valores válidos: Easy, Medium, Hard.")
    };

    public override string ToString() => $"{Name} (x{ScoreMultiplier})";
}