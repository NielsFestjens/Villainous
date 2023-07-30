using System.Diagnostics;

namespace Villainous.Server.Game.Villains;

[DebuggerDisplay("{Type} {Name}")]
public abstract class CardInfo
{
    protected CardInfo(string villainNameName, CardType type, string name, int? cost, int? strength, int amount, string description)
    {
        VillainName = villainNameName;
        Type = type;
        Name = name;
        Cost = cost;
        Strength = strength;
        Amount = amount;
        Description = description;
    }

    public string VillainName { get; set; }
    public string Name { get; set; }
    public CardType Type { get; set; }
    public virtual bool WikiNameIncludesVillain => false;
    public virtual string WikiName => $"{Name}{(WikiNameIncludesVillain ? $" {VillainName}" : "")}";
    public string Description { get; set; }
    public int? Cost { get; set; }
    public int? Strength { get; set; }
    public int Amount { get; set; }

    public LocationType LocationType => CardTypes.VillainCardTypes.Contains(Type) ? LocationType.Ally : LocationType.Hero;
    public CardInfoDto GetInfoDto() => new(Name, Type, WikiName, Description, Cost, Strength, Amount);

    public virtual int? GetStrengthBonus(Player player, Card card) => 0;

    public virtual bool CanActivate(Player player, Card card, ActivatableMomentType moment) => false;
    public virtual Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment) => Task.CompletedTask;
}

public abstract class VillainCardInfo : CardInfo
{
    protected VillainCardInfo(string villainName, CardType type, string name, int cost, int? strength, int amount, string description) : base(villainName, type, name, cost, strength, amount, description) { }
}

public abstract class FateCardInfo : CardInfo
{
    protected FateCardInfo(string villainName, CardType type, string name, int? strength, int amount, string description) : base(villainName, type, name, null, strength, amount, description) { }
}

public enum ActivatableMomentType
{
    BeforeVillainMove = 1,
    OnReceive,
    OnPlay,
    OnFate,
    OnCardActivation ,
    OnCondition
}