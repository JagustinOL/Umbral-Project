namespace SessionManagement.Domain.ValueObjects;

public record Points
{
    public int Value { get; init; }

    public Points(int value)
    {
        if (value < 0) 
            throw new ArgumentException("Los puntos no pueden ser negativos en esta asignación.");
        Value = value;
    }

    public static Points Zero => new(0);

    // Operadores para permitir matemáticas fáciles en el dominio (C# moderno)
    public static Points operator +(Points a, Points b) => new(a.Value + b.Value);
    public static Points operator -(Points a, Points b) => new(Math.Max(0, a.Value - b.Value));
}