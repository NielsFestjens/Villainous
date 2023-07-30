namespace Villainous.Server.Game.Villains.Editions.TheWorstTakesItAll;
using static ActionType;

public class TheWorstTakesItAllInfo
{
    public const string Name = "The Worst Takes It Al";
}

public class CaptainHookVillainInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public override string Name => "Captain Hook";
    public override string Objective => "Defeat Peter Pan at the Jolly Roger.";
    public override bool CheckObjective(Player player, bool isStartOfTurn) => false;

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
    };
}

public class JafarVillainInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public override string Name => "Jafar";
    public override string Objective => "Start your turn with the Magic Lamp at Sultan's Palace and the Genie under your control.";
    public override bool CheckObjective(Player player, bool isStartOfTurn) => false;

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
    };
}

public class PrinceJohnVillainInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public override string Name => "Prince John";
    public override string Objective => "Start your turn with at least 20 Power.";
    public override bool CheckObjective(Player player, bool isStartOfTurn) => false;

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
    };
}

public class QueenOfHeartsVillainInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public override string Name => "Queen of Hearts";
    public override string Objective => "Have a Wicked at each location and successfully take a shot.";
    public override bool CheckObjective(Player player, bool isStartOfTurn) => false;

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
    };
}

public class UrsulaVillainInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public override string Name => "Ursula";
    public override string Objective => "Start your turn with the Trident and the Crown at Ursula's Lair.";
    public override bool CheckObjective(Player player, bool isStartOfTurn) => false;

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
        new()
        {
            Name = "Name",
            HeroLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
            AllyLocationActions = new List<ActionInfo> { new(PlayCard), new(PlayCard), },
        },
    };
}