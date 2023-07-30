namespace Villainous.ClientCmd;

public class VillainousClientConfig
{
    public string BaseAddress { get; set; } = null!;
    public string? Username { get; set; }
    public bool IsFirstUser { get; set; }
}