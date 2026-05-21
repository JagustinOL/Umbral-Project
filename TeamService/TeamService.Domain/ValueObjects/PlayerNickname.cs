namespace TeamService.Domain.ValueObjects;

public record PlayerNickname
{
    public string Value { get; init; }

    private PlayerNickname(string value) => Value = value;

    public static PlayerNickname Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 3 or > 15)
            throw new ArgumentException("El nickname debe tener entre 3 y 15 caracteres.");
            
        return new PlayerNickname(value);
    }
}