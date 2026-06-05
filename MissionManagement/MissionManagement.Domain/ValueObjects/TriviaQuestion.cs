namespace MissionManagement.Domain.ValueObjects;

public sealed record TriviaQuestion
{
    public string Prompt { get; init; }
    public IReadOnlyList<string> Options { get; init; }
    public int CorrectOptionIndex { get; init; }

    public TriviaQuestion(string prompt, IReadOnlyList<string> options, int correctOptionIndex)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("La pregunta de trivia no puede estar vacia.", nameof(prompt));
        if (options is null || options.Count < 2)
            throw new ArgumentException("La trivia debe tener al menos dos opciones.", nameof(options));
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Las opciones de trivia no pueden estar vacias.", nameof(options));
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new ArgumentOutOfRangeException(nameof(correctOptionIndex),
                "El indice de respuesta correcta debe existir dentro de las opciones.");

        Prompt = prompt.Trim();
        Options = options.Select(x => x.Trim()).ToList().AsReadOnly();
        CorrectOptionIndex = correctOptionIndex;
    }
}
