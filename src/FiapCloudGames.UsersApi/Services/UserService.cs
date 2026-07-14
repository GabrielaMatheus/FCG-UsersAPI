using System.Text.RegularExpressions;
using FiapCloudGames.Contracts;
using FiapCloudGames.UsersApi.Data;
using FiapCloudGames.UsersApi.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.UsersApi.Services;

public sealed partial class UserService(
    UsersDbContext db,
    PasswordHasher passwordHasher,
    IPublishEndpoint publisher)
{
    public async Task<User> CreateAsync(
        string name,
        string email,
        string password,
        UserRole role = UserRole.User,
        bool publishEvent = true,
        CancellationToken cancellationToken = default)
    {
        ValidateEmail(email);
        var normalizedEmail = User.NormalizeEmail(email);
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            throw new ConflictException("Ja existe um usuario com este e-mail.");

        var user = new User(name, normalizedEmail, passwordHasher.Hash(password), role);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        if (publishEvent)
        {
            await publisher.Publish(
                new UserCreatedEvent(user.Id, user.Name, user.Email, DateTime.UtcNow),
                cancellationToken);
        }

        return user;
    }

    public async Task<User> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new UnauthorizedAccessException("E-mail ou senha invalidos.");

        var normalizedEmail = User.NormalizeEmail(email);
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha invalidos.");
        return user;
    }

    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<User>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task SeedAdministratorAsync(CancellationToken cancellationToken = default)
    {
        var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;
        if (!await db.Users.AnyAsync(x => x.Email == User.NormalizeEmail(email), cancellationToken))
            await CreateAsync("Administrador", email, password, UserRole.Administrator, false, cancellationToken);
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailPattern().IsMatch(email))
            throw new ArgumentException("Formato de e-mail invalido.");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}

public sealed class ConflictException(string message) : Exception(message);
