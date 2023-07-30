using static Villainous.CardType;

namespace Villainous;

public record PlayerInfoDto(int Index, Guid UserId, VillainInfoDto VillainInfo);
public record VillainInfoDto(string Edition, string Name, List<LocationInfoDto> Locations, List<CardInfoDto> Cards);
public record CardInfoDto(string Name, CardType Type, string WikiName, string Description, int? Cost, int? Strength, int Amount);
public record LocationInfoDto(string Name, List<ActionInfoDto> HeroLocationActions, List<ActionInfoDto> AllyLocationActions, bool StartsLocked);
public record ActionInfoDto(ActionType Type, int? Amount);

public record GameState(PlayerState Player, List<PlayerPublicState> OtherPlayers);
public record PlayerState(int Power, int VillainLocationIndex, List<CardState> Hand, List<LocationState> Locations);
public record CardState(string Name, List<int?> Cost, int? Strength, List<CardState> Cards, CardLocation? CardLocation, CardType Type);
public record LocationState(int Index, string Name, IEnumerable<ActionState> Actions, List<CardState> AllyCards, List<CardState> HeroCards);
public record ActionState(int Index, ActionType Type, bool IsAvailable);
public record PlayerPublicState(Guid Id, int Power, int VillainLocationIndex, List<CardState> Hand, List<LocationState> Locations);

public record CardLocation(int LocationIndex, LocationType Type, int? CardIndex);

public enum LocationType
{
    Hero = 1,
    Ally = 2
}

public enum ActionType
{
    GainPower = 1,
    MoveItemOrAlly,
    MoveHero,
    PlayCard,
    Fate,
    DiscardCards,
    Vanquish,
    Activate
}
public enum CardType
{
    Ally = 1,
    AllyItem,
    VillainItem,
    VillainEffect,
    Condition,
    VillainSpecial,

    Hero = 11,
    HeroItem,
    FateEffect,
}

public static class CardTypes
{
    public static readonly IReadOnlyCollection<CardType> VillainCardTypes = new[] { Ally, AllyItem, VillainItem, VillainEffect, Condition, VillainSpecial }.AsReadOnly();
    public static readonly IReadOnlyCollection<CardType> FateCardTypes = new[] { Hero, HeroItem, FateEffect }.AsReadOnly();
}