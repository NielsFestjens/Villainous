using System.Numerics;
using static Villainous.ActionType;
using static Villainous.Server.Game.Villains.ActivatableMomentType;

namespace Villainous.Server.Game.Villains.Editions.TheWorstTakesItAll.Maleficent;

public class MaleficentInfo : VillainInfo
{
    public override string Edition => TheWorstTakesItAllInfo.Name;
    public const string VillainName = "Maleficent";
    public override string Name => VillainName;
    public override string CardTypeVillainSpecialName => "Curse";
    public override string Objective => "Start your turn with a Curse at each location.";

    public override bool CheckObjective(Player player, bool isStartOfTurn)
    {
        return isStartOfTurn && player.Locations.All(x => x.HasCardOfType(CardType.VillainSpecial));
    }

    public override List<LocationInfo> Locations => new()
    {
        new()
        {
            Name = "Forbidden Mountains",
            HeroLocationActions = new List<ActionInfo>
            {
                new(MoveItemOrAlly),
                new(PlayCard),
            },
            AllyLocationActions = new List<ActionInfo>
            {
                new(GainPower, 1),
                new(PlayCard),
            },
        },
        new()
        {
            Name = "Briar Rose's Cottage",
            HeroLocationActions = new List<ActionInfo>
            {
                new(GainPower, 2),
                new(MoveItemOrAlly),
            },
            AllyLocationActions = new List<ActionInfo>
            {
                new(PlayCard),
                new(DiscardCards),
            },
        },
        new()
        {
            Name = "The Forest",
            HeroLocationActions = new List<ActionInfo>
            {
                new(DiscardCards),
                new(PlayCard),
            },
            AllyLocationActions = new List<ActionInfo>
            {
                new(GainPower, 3),
                new(PlayCard),
            },
        },
        new()
        {
            Name = "King Stefan's Castle",
            HeroLocationActions = new List<ActionInfo>
            {
                new(GainPower, 1),
                new(Fate),
            },
            AllyLocationActions = new List<ActionInfo>
            {
                new(Vanquish),
                new(PlayCard),
            },
        },
    };
}

public abstract class MaleficentVillainCardInfo : VillainCardInfo
{
    protected MaleficentVillainCardInfo(CardType type, string name, int cost, int? strength, int amount, string description) : base(MaleficentInfo.VillainName, type, name, cost, strength, amount, description) { }
}

public abstract class MaleficentFateCardInfo : FateCardInfo
{
    protected MaleficentFateCardInfo(CardType type, string name, int? strength, int amount, string description) : base(MaleficentInfo.VillainName, type, name, strength, amount, description) { }
}

public class CacklingGoon : MaleficentVillainCardInfo
{
    public CacklingGoon() : base(CardType.Ally, "Cackling Goon", 1, 1, 3, "Cackling Goon gets +1 Strength for each Hero at his location.") { }

    public override int? GetStrengthBonus(Player player, Card card)
    {
        return player.GetLocation(card)?.GetHeroes().Count;
    }
}

public class SavageGoon : MaleficentVillainCardInfo
{
    public SavageGoon() : base(CardType.Ally, "Savage Goon", 3, 4, 3, "No additional Ability.") { }
}

public class SinisterGoon : MaleficentVillainCardInfo
{
    public SinisterGoon() : base(CardType.Ally, "Sinister Goon", 2, 3, 3, "Sinister Goon gets +1 Strength if there are any Curses at his location.") { }

    public override int? GetStrengthBonus(Player player, Card card)
    {
        return player.GetLocation(card)?.GetAllyCards(CardType.VillainSpecial).Any() == true ? 1 : null;
    }
}

public class Raven : MaleficentVillainCardInfo
{
    public Raven() : base(CardType.Ally, "Raven", 3, 1, 1, "Before Maleficent moves, you may move Raven to any location and perform one available action at his new location. Raven cannot perform Fate actions.") { }
    
    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == BeforeVillainMove;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        var performSpecial = await gameHub.AskFromPlayer(player, x => x.ChoosePerformSpecial(card.GetState(player)));
        if (!performSpecial)
            return;

        var availableLocations = player.Locations.Where(x => x.Index != card.LocationIndex).ToList();
        var locationIndex = await gameHub.AskFromPlayer(player, x => x.ChooseSpecialLocation(card.GetState(player), availableLocations.Select(y => y.GetState(player)).ToList()));

        var location = availableLocations[locationIndex];
        var availableActions = location.GetActionStates(player);
        var action = await gameHub.AskFromPlayer(player, x => x.ChooseSpecialAction(card.GetState(player), availableActions));

        await player.PerformSpecialAction(gameHub, locationIndex, action);
    }
}

public class DragonForm : MaleficentVillainCardInfo
{
    public DragonForm() : base(CardType.VillainEffect, "Dragon Form", 1, null, 3, "Defeat a Hero with a Strength of 3 or less. If a Fate action targets you before your next turn, gain 3 Power.") { }
    
    private static List<Card> GetDefeatableHeroes(Player player) => player.GetHeroes().Where(x => x.GetStrength(player) <= 3).ToList();

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay && GetDefeatableHeroes(player).Any();

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        var defeatableHeroes = GetDefeatableHeroes(player);
        var defeatableHeroIndex = await gameHub.AskFromPlayer(player, x => x.ChooseHeroToVanquish(defeatableHeroes.Select(y => y.GetState(player)).ToList()));
        var hero = defeatableHeroes[defeatableHeroIndex];

        player.DiscardCardFromLocation(hero);

        player.Game.EventHandler.RegisterOnSelf<FatedEvent>(player, card, HandleFated, Duration.UntilStartOfNextTurn);
    }


    private static void HandleFated(Player player, Card card, FatedEvent fatedEvent) => player.AddPower(3);
}

public class Vanish : MaleficentVillainCardInfo
{
    public Vanish() : base(CardType.VillainEffect, "Vanish", 0, null, 3, "At the start of your next turn, Maleficent does not have to move to a new location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateNeedsToMoveEvent, bool>(player, card, HandleCalculateNeedsToMove, Duration.UntilAfterVillainMovePhase);
        await Task.CompletedTask;
    }

    private static bool HandleCalculateNeedsToMove(Player player, Card card, CalculateNeedsToMoveEvent theEvent) => false;
}

public class ForestOfThorns : MaleficentVillainCardInfo
{
    public ForestOfThorns() : base(CardType.VillainSpecial, "Forest of Thorns", 3, null, 3, "Heroes must have a Strength of 4 or more to be played to this location.\r\nDiscard this Curse when a Hero is played to this location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateCanFateEvent, bool>(player, card, HandleCanFate);
        player.Game.EventHandler.RegisterOnSelf<FatedEvent>(player, card, HandleFated);
        await Task.CompletedTask;
    }

    private static bool HandleCanFate(Player player, Card card, CalculateCanFateEvent theEvent)
    {
        return theEvent.Location.LocationIndex != card.LocationIndex || !theEvent.Card.IsHero || theEvent.Card.GetStrength(player) >= 4;
    }

    private static void HandleFated(Player player, Card card, FatedEvent theEvent)
    {
        if (theEvent.Card.LocationIndex == card.LocationIndex)
            player.DiscardCardFromLocation(card);
    }
}

public class GreenFire : MaleficentVillainCardInfo
{
    public GreenFire() : base(CardType.VillainSpecial, "Green Fire", 3, null, 3, "Heroes cannot be played to this location.\r\nDiscard this Curse if Maleficent moves to this location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateCanFateEvent, bool>(player, card, HandleCanFate);
        player.Game.EventHandler.RegisterOnSelf<VillainMovedEvent>(player, card, HandleVillainMoved);
        await Task.CompletedTask;
    }

    private static bool HandleCanFate(Player player, Card card, CalculateCanFateEvent theEvent)
    {
        return theEvent.Location.LocationIndex != card.LocationIndex || !theEvent.Card.IsHero;
    }

    private static void HandleVillainMoved(Player player, Card card, VillainMovedEvent theEvent)
    {
        if (theEvent.Location.Index == card.LocationIndex)
            player.DiscardCardFromLocation(card);
    }
}

public class DreamlessSleep : MaleficentVillainCardInfo
{
    public DreamlessSleep() : base(CardType.VillainSpecial, "Dreamless Sleep", 3, null, 2, "Heroes at this location get -2 Strength.\r\nDiscard this Curse when an Ally is played to this location.") { }
    
    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateCardStrengthBonusEvent, int?>(player, card, HandleCalculateCardStrengthBonus);
        player.Game.EventHandler.RegisterOnSelf<CardPlayedEvent>(player, card, HandleCardPlayed);
        await Task.CompletedTask;
    }

    private static int? HandleCalculateCardStrengthBonus(Player player, Card card, CalculateCardStrengthBonusEvent theEvent)
    {
        return theEvent.Card.IsHero && theEvent.Card.LocationIndex == card.LocationIndex ? -2 : 0;
    }

    private static void HandleCardPlayed(Player player, Card card, CardPlayedEvent theEvent)
    {
        if (theEvent.Card.LocationIndex == card.LocationIndex && theEvent.Card.Type == CardType.Ally)
            player.DiscardCardFromLocation(card);
    }
}

public class Malice : MaleficentVillainCardInfo
{
    public Malice() : base(CardType.Condition, "Malice", 2, null, 2, "During their turn, if another player defeats a Hero with a Strength of 4 or more, you may play Malice. Defeat a Hero with a Strength of 4 or less.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnReceive;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnOthersAsync<HeroVanquishedEvent>(player, card, HandleHeroVanquished);
        await Task.CompletedTask;
    }

    private static async Task HandleHeroVanquished(IGameHub gameHub, Player player, Card card, HeroVanquishedEvent theEvent)
    {
        if (theEvent.HeroStrength < 4)
            return;

        var defeatableHeroes = player.GetDefeatableHeroes().Where(x => x.GetStrength(player) <= 4).ToList();
        if (!defeatableHeroes.Any())
            return;

        if (!await gameHub.AskFromPlayer(player, x => x.ActivateCondition(card.GetState(player))))
            return;

        player.RemoveCardFromHand(card);

        var defeatableHeroIndex = await gameHub.AskFromPlayer(player, x => x.ChooseHeroToVanquish(defeatableHeroes.Select(y => y.GetState(player)).ToList()));
        var hero = defeatableHeroes[defeatableHeroIndex];

        await Action.VanquishHero(gameHub, player, hero);

        player.AddToVillainDiscardPile(card);
    }
}

public class Tyranny : MaleficentVillainCardInfo
{
    public Tyranny() : base(CardType.Condition, "Tyranny", 2, null, 2, "During their turn, if another player has three or more Allies in their Realm, you may play Tyranny. Draw three cards into your hand, then discard any three cards.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnCondition && player.Game.CurrentPlayer.GetAllies().Count >= 3;
    
    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await player.DrawHandCards(gameHub, 3);
        var cards = player.GetHand();
        var cardIndexes = await gameHub.AskFromPlayer(player, x => x.ChooseCardsToDiscard(cards.Select(y => y.GetState(player)).ToList(), 3, 3));
        if (cardIndexes.Distinct().Count() != 3)
            throw new Exception("Pick exactly 3 cards");
        
        player.DiscardCardsFromHand(cardIndexes);
    }
}

public class SpinningWheel : MaleficentVillainCardInfo
{
    public SpinningWheel() : base(CardType.VillainItem, "Spinning Wheel", 1, null, 1, "If a Hero is defeated at this location, gain Power equal to the Hero's Strength minus 1.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<HeroVanquishedEvent>(player, card, HandleHeroVanquished);
        await Task.CompletedTask;
    }

    private static void HandleHeroVanquished(Player player, Card card, HeroVanquishedEvent theEvent)
    {
        if (theEvent.Card.LocationIndex == card.LocationIndex && theEvent.Card.Type == CardType.Hero)
            player.AddPower(theEvent.Card.GetStrength(player)!.Value - 1);
    }
}

public class Staff : MaleficentVillainCardInfo
{
    public Staff() : base(CardType.VillainItem, "Staff", 1, null, 1, "If Maleficent is at this location, the Cost to play an Effect or Curse is reduced by 1 Power.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnPlay;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateCardCostBonusEvent, int?>(player, card, HandleCalculateCardCostBonus);
        await Task.CompletedTask;
    }

    private static int? HandleCalculateCardCostBonus(Player player, Card card, CalculateCardCostBonusEvent theEvent)
    {
        return theEvent.Location?.Index == card.LocationIndex && player.LocationIndex == card.LocationIndex && theEvent.Card.Type is CardType.VillainEffect or CardType.VillainSpecial ? -1 : null;
    }
}

public class Guards : MaleficentFateCardInfo
{
    public Guards() : base(CardType.Hero, "Guards", 3, 3, "When performing a Vanquish action to defeat Guards, at least two Allies must be used.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        player.Game.EventHandler.RegisterOnSelf<CalculateRequiredAllyCountEvent, int>(player, card, HandleCalculateRequiredAllyCount);
        await Task.CompletedTask;
    }

    private static int HandleCalculateRequiredAllyCount(Player player, Card card, CalculateRequiredAllyCountEvent theEvent) => 2;
}

public class SwordOfTruth : MaleficentFateCardInfo
{
    public SwordOfTruth() : base(CardType.HeroItem, "Sword of Truth", 2, 3, "When Sword of Truth is played, attach it to a Hero with no other attached Items. That Hero gets +2 Strength. The Cost to play a Curse to this location is increased by 2 Power.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment is OnReceive or OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;

        if (moment == OnReceive)
            player.Game.EventHandler.RegisterOnSelf<CalculateCanFateEvent, bool>(player, card, HandleCalculateCanFate);

        if (moment == OnFate)
            player.Game.EventHandler.RegisterOnSelf<CalculateCardCostBonusEvent, int?>(player, card, HandleCalculateCardCostBonus);
    }

    private static bool HandleCalculateCanFate(Player player, Card card, CalculateCanFateEvent theEvent)
    {
        return theEvent.Card != card || player.GetHeroes().Any(x => x.Cards.All(y => y.Type != CardType.HeroItem));
    }

    private static int? HandleCalculateCardCostBonus(Player player, Card card, CalculateCardCostBonusEvent theEvent)
    {
        return theEvent.Location?.Index == card.LocationIndex && theEvent.Card.Type == CardType.VillainSpecial ? 2 : null;
    }
}

public class OnceUponADream : MaleficentFateCardInfo
{
    public OnceUponADream() : base(CardType.FateEffect, "Once Upon a Dream", null, 2, "Discard a Curse from a location in Maleficent's Realm that has a Hero.") { }

    private List<Location> GetLocationsWithHeroesAndCurses(Player player) => player.Locations.Where(x => x.GetHeroes().Any() && x.GetAllyCards(CardType.VillainSpecial).Any()).ToList();

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate && GetLocationsWithHeroesAndCurses(player).Any();

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;

        var locations = GetLocationsWithHeroesAndCurses(player);
        var locationIndex = await gameHub.AskFromPlayer(player.Game.CurrentPlayer, x => x.ChooseSpecialLocation(card.GetState(player), locations.Select(y => y.GetState(player)).ToList()));

        var curses = locations[locationIndex].GetAllyCards(CardType.VillainSpecial).ToList();
        var curseIndex = await gameHub.AskFromPlayer(player.Game.CurrentPlayer, x => x.ChooseSpecialCard(card.GetState(player), curses.Select(y => y.GetState(player)).ToList()));
        if (curseIndex == null)
            throw new Exception();

        var curse = curses[curseIndex.Value];

        player.DiscardCardFromLocation(curse);
    }
}

public class Aurora : MaleficentFateCardInfo
{
    public Aurora() : base(CardType.Hero, "Aurora", 4, 1, "When Aurora is played, reveal the top card of Maleficent's Fate deck. If it is a Hero, play it. Otherwise, return it to the top of the deck.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        var fateCards = player.DrawFateCards(1);
        if (!fateCards.Any())
            return;

        var fateCard = fateCards[0];
        if (fateCard.IsHero)
        {
            await Action.PlayFateCard(gameHub, player.Game.CurrentPlayer, player, fateCards, fateCard);
        }
        else
        {
            player.AddToTopOfFateDeck(fateCard);
        }
    }
}

public class Fauna : MaleficentFateCardInfo
{
    public Fauna() : base(CardType.Hero, "Fauna", 2, 1, "When Fauna is played, you may discard Dreamless Sleep from her location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;
        var dreamlessSleep = player.GetLocation(card)!.GetAllyCards(CardType.VillainSpecial).FirstOrDefault(x => x.Is<DreamlessSleep>());
        if (dreamlessSleep != null)
            player.DiscardCardFromLocation(dreamlessSleep);
    }
}

public class Flora : MaleficentFateCardInfo
{
    public Flora() : base(CardType.Hero, "Flora", 3, 1, "When Flora is played, Maleficent must reveal her hand.\r\nUntil Flora is defeated, Maleficent must play with her hand revealed.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;
        player.Game.EventHandler.RegisterOnSelf<CalculateAreCardsRevealedEvent, bool>(player, card, HandleCalculateAreCardsRevealed);
    }

    private static bool HandleCalculateAreCardsRevealed(Player player, Card card, CalculateAreCardsRevealedEvent theEvent)
    {
        return true;
    }
}

public class KingHubert : MaleficentFateCardInfo
{
    public KingHubert() : base(CardType.Hero, "King Hubert", 3, 1, "When King Hubert is played, you may move one Ally from each adjacent location to his location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;
        var locationIndex = card.LocationIndex!.Value;
        await MoveAllies(gameHub, player, card, locationIndex - 1);

        await MoveAllies(gameHub, player, card, locationIndex + 1);
    }

    private static async Task MoveAllies(IGameHub gameHub, Player player, Card card, int locationIndex)
    {
        if (locationIndex < 0 || locationIndex >= player.Locations.Count)
            return;

        var possibleAllies = player.Locations[locationIndex].GetAllies();
        if (!possibleAllies.Any())
            return;

        var possibleAllyIndex = await gameHub.AskFromPlayer(player.Game.CurrentPlayer, x => x.ChooseSpecialCard(card.GetState(player), possibleAllies.Select(y => y.GetState(player)).ToList()));
        if (possibleAllyIndex == null)
            return;

        var ally = possibleAllies[possibleAllyIndex.Value];
        player.MoveCard(ally, card.LocationIndex!.Value);
    }
}

public class KingStefan : MaleficentFateCardInfo
{
    public KingStefan() : base(CardType.Hero, "King Stefan", 4, 1, "When King Stefan is played, you may move Maleficent to any location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await player.MoveVillain(gameHub, player.Game.CurrentPlayer, true);
    }
}

public class Merryweather : MaleficentFateCardInfo
{
    public Merryweather() : base(CardType.Hero, "Merryweather", 4, 1, "Curses cannot be played to Merryweather's location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;

    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        await Task.CompletedTask;
        player.Game.EventHandler.RegisterOnSelf<CalculateCanPlayCardEvent, bool>(player, card, HandleCalculateCanPlayCard);
    }

    private static bool HandleCalculateCanPlayCard(Player player, Card card, CalculateCanPlayCardEvent theEvent)
    {
        return theEvent.Card.Type != CardType.VillainSpecial || theEvent.Location.Index != card.LocationIndex;
    }
}

public class PrincePhillip : MaleficentFateCardInfo
{
    public PrincePhillip() : base(CardType.Hero, "Prince Phillip", 5, 1, "When Prince Phillip is played, you may discard all Allies from his location.") { }

    public override bool CanActivate(Player player, Card card, ActivatableMomentType moment) => moment == OnFate;
    
    public override async Task Activate(IGameHub gameHub, Player player, Card card, ActivatableMomentType moment)
    {
        var performSpecial = await gameHub.AskFromPlayer(player.Game.CurrentPlayer, x => x.ChoosePerformSpecial(card.GetState(player)));
        if (!performSpecial)
            return;

        var allies = player.GetLocation(card)!.GetAllies();
        player.DiscardCardsFromLocation(allies);
    }
}