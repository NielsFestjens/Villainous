namespace Villainous.ClientCmd;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public bool IsAvailable { get; set; }
}