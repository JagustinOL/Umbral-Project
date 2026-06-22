namespace UserService.Application.Common;

public static class PasswordValidation
{
    private const int MinLength = 8;

    public static void EnsureValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            throw new ArgumentException(
                $"La contraseña debe tener al menos {MinLength} caracteres.",
                nameof(password));
        }
    }
}
