namespace Villainous.Server.Game;

public class User
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = null!;
    public string ConnectionId { get; set; } = null!;
    public Guid? GameId { get; set; }

    public override string ToString() => $"{DisplayName} {Id} {ConnectionId}";
}

public class UserCredentials
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public byte[] PasswordSalt { get; init; } = null!;
    public string PasswordHash { get; init; } = null!;
}