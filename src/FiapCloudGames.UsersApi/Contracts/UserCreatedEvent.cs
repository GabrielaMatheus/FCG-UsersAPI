namespace FiapCloudGames.Contracts;

public sealed record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime OccurredAtUtc);
