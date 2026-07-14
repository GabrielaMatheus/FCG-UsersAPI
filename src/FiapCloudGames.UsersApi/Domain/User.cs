namespace FiapCloudGames.UsersApi.Domain;

public sealed class User
{
    private User() { }

    public User(string name, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome e obrigatorio.", nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public enum UserRole
{
    User = 1,
    Administrator = 2
}
