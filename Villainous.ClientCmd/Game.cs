namespace Villainous.ClientCmd;

public class Game
{
    public string VillainName { get; set; } = null!;
    public List<PlayerInfoDto> Players { get; set; } = null!;
    public VillainInfoDto VillainInfo { get; set; } = null!;

    public int VillainLocationIndex { get; set; }
    public int Power { get; set; }
    public List<CardState> Hand { get; set; } = null!;
    public CardState? VanquishingHero { get; set; }
    public PlayerState PlayerState { get; set; } = null!;

    public bool IsVillain(string villainName) => VillainName == villainName;
}