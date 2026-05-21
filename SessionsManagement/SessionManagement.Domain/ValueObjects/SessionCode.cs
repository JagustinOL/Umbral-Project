namespace SessionManagement.Domain.ValueObjects;

public record SessionCode
{
    public string Value { get; init; }

    private SessionCode(string value) => Value = value;

    public static SessionCode Generate()
    {
        var code = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return new SessionCode(code);
    }

    public static SessionCode FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 6)
            throw new ArgumentException("El código de sesión debe tener exactamente 6 caracteres.");
            
        return new SessionCode(value.ToUpper());
    }
}