namespace Villainous.Server.Game.Villains;

public abstract class VillainInfo
{
    public abstract string Edition { get; }
    public abstract string Name { get; }
    public virtual string? CardTypeVillainSpecialName => null;
    public abstract string Objective { get; }
    public abstract bool CheckObjective(Player player, bool isStartOfTurn);
    public abstract List<LocationInfo> Locations { get; }

    public virtual bool StartsWithLock => false;

    public VillainInfoDto GetInfo(List<CardInfoDto> cards) => new(Edition, Name, Locations.Select(x => x.GetInfoDto()).ToList(), cards);
}

public class LocationInfo
{
    public string Name { get; set; } = null!;
    public List<ActionInfo> HeroLocationActions { get; set; } = null!;
    public List<ActionInfo> AllyLocationActions { get; set; } = null!;
    public bool StartsLocked { get; set; }

    public LocationInfoDto GetInfoDto() => new(Name, HeroLocationActions.Select(x => x.GetInfoDto()).ToList(), AllyLocationActions.Select(x => x.GetInfoDto()).ToList(), StartsLocked);
}

public class ActionInfo
{
    public ActionType Type { get; set; }
    public int? Amount { get; set; }

    public ActionInfo(ActionType type, int? amount = null)
    {
        Type = type;
        Amount = amount;
    }

    public ActionInfoDto GetInfoDto() => new(Type, Amount);
}