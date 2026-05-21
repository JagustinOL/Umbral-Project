namespace TeamService.Domain.ValueObjects;

public record TeamCode
{
    public string Value { get; init; }

    private TeamCode(string value) => Value = value;

    public static TeamCode Generate()
    {
        // Genera un código aleatorio de 6 caracteres (Ej: A4F9X2)
        var code = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return new TeamCode(code);
    }

    public static TeamCode FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 4 or > 8)
            throw new ArgumentException("El código de acceso debe tener entre 4 y 8 caracteres.");
            
        return new TeamCode(value.ToUpper());
    }
}