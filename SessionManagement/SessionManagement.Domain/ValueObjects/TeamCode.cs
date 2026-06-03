using System.Text.RegularExpressions;

namespace SessionManagement.Domain.ValueObjects;

/// <summary>
/// Value Object que representa el código único de acceso de un equipo.
/// Los participantes usan este código para unirse a su equipo desde el frontend.
///
/// Formato: 6 caracteres alfanuméricos en mayúsculas (ej: "AB12CD").
/// La inmutabilidad garantiza que el código nunca cambia una vez asignado.
/// </summary>
public sealed record TeamCode
{
    private static readonly Regex ValidPattern =
        new(@"^[A-Z0-9]{6}$", RegexOptions.Compiled);

    public string Value { get; }

    private TeamCode(string value) => Value = value;

    /// <summary>
    /// Genera un nuevo TeamCode aleatorio y único.
    /// Llamado al crear un Team nuevo.
    /// </summary>
    public static TeamCode Generate()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(
            Enumerable.Range(0, 6)
                      .Select(_ => chars[random.Next(chars.Length)])
                      .ToArray());
        return new TeamCode(code);
    }

    /// <summary>
    /// Reconstruye un TeamCode desde persistencia o desde una request.
    /// Valida el formato antes de crear.
    /// </summary>
    public static TeamCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El código de equipo no puede estar vacío.", nameof(value));

        var upper = value.Trim().ToUpperInvariant();

        if (!ValidPattern.IsMatch(upper))
            throw new ArgumentException(
                $"El código de equipo '{value}' no tiene el formato válido (6 caracteres alfanuméricos).",
                nameof(value));

        return new TeamCode(upper);
    }

    public override string ToString() => Value;
}