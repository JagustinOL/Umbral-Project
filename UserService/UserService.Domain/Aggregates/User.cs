using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Aggregates;

public sealed class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    private User() { }

    private User(Guid id, string email, string firstName, string lastName, UserRole role, UserStatus status)
        : base(id)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        Status = status;
    }

    public static User Create(
        Guid id,
        string email,
        string firstName,
        string lastName,
        UserRole role,
        UserStatus status = UserStatus.Active)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El Id del usuario no puede ser vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo es obligatorio.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

        return new User(
            id,
            email.Trim(),
            firstName.Trim(),
            lastName.Trim(),
            role,
            status);
    }

    public bool IsActive => Status == UserStatus.Active;

    public void Activate() => Status = UserStatus.Active;

    public void Deactivate() => Status = UserStatus.Inactive;

    public void UpdateProfile(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo es obligatorio.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

        Email = email.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
}
