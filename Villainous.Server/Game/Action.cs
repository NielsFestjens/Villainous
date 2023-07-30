namespace Villainous.Server.Game;

public class Action
{
    private readonly ActionInfo _actionInfo;
    public int Index { get; }
    public bool IsCoveredByHero { get; }
    public bool HasBeenPerformed { get; }
    public bool IsPossible { get; }
    public bool IsAvailable => !IsCoveredByHero && IsPossible && !HasBeenPerformed;
    public ActionType Type => _actionInfo.Type;
    public int? Amount => _actionInfo.Amount;

    public Action(ActionInfo actionInfo, int index, ICollection<int> performedActions, bool isCoveredByHero, bool isPossible)
    {
        _actionInfo = actionInfo;
        Index = index;
        IsCoveredByHero = isCoveredByHero;
        HasBeenPerformed = performedActions.Contains(Index);
        IsPossible = isPossible;
    }

    public async Task Start(IGameHub gameHub, Player player)
    {
        Func<IGameHub, Player, Task> executable = Type switch
        {
            ActionType.GainPower => (_, _) => GainPower(player, Amount!.Value),
            ActionType.MoveItemOrAlly => MoveItemOrAlly,
            ActionType.MoveHero => MoveHero,
            ActionType.PlayCard => PlayCard,
            ActionType.Fate => Fate,
            ActionType.DiscardCards => DiscardCards,
            ActionType.Vanquish => VanquishHero,
            ActionType.Activate => Activate,
            _ => throw new Exception("Unknown action type"),
        };
        await executable(gameHub, player);
    }

    private static async Task GainPower(Player player, int amount)
    {
        player.AddPower(amount);
        await Task.CompletedTask;
    }

    private static async Task MoveItemOrAlly(IGameHub gameHub, Player player)
    {
        var itemsAndAllies = player.GetMovableAlliesAndItems();
        var itemOrAllyIndex = await gameHub.AskFromPlayer(player, x => x.ChooseItemOrAllyToMove(itemsAndAllies.Select(y => y.GetState(player)).ToList()));
        var itemOrAlly = itemsAndAllies[itemOrAllyIndex];

        var possibleLocations = player.Locations.Where(x => x.Index != itemOrAlly.LocationIndex && !x.IsLocked).ToList();
        var possibleLocationIndex = await gameHub.AskFromPlayer(player, x => x.ChooseLocationToMoveItemOrAllyTo(possibleLocations.Select(y => y.GetState(player)).ToList()));
        
        player.MoveCard(itemOrAlly, possibleLocations[possibleLocationIndex].Index);
    }

    private static async Task MoveHero(IGameHub gameHub, Player player)
    {
        var heroes = player.GetMovableHeroes();
        var heroIndex = await gameHub.AskFromPlayer(player, x => x.ChooseHeroToMove(heroes.Select(y => y.GetState(player)).ToList()));
        var hero = heroes[heroIndex];

        var possibleLocations = player.Locations.Where(x => x.Index != hero.LocationIndex && !x.IsLocked).ToList();
        var possibleLocationIndex = await gameHub.AskFromPlayer(player, x => x.ChooseLocationToMoveHeroTo(possibleLocations.Select(y => y.GetState(player)).ToList()));

        player.MoveCard(hero, possibleLocations[possibleLocationIndex].Index);
    }

    private static async Task PlayCard(IGameHub gameHub, Player player)
    {
        var cards = player.GetHand().Where(x => player.CanPlayCard(x)).ToList();
        var index = await gameHub.AskFromPlayer(player, x => x.ChooseCardToPlay(cards.Select(x => x.GetState(player)).ToList()));
        
        var card = cards[index];

        if (card.Type is CardType.VillainEffect)
        {
            await player.PlayCard(gameHub, card, null);
            return;
        }

        var possibleLocations = player.Locations.Where(x => x.CanPlayCard(card, player)).ToList();
        var possibleLocationIndex = await gameHub.AskFromPlayer(player, x => x.ChooseLocationToPlayCardTo(possibleLocations.Select(y => y.GetState(player)).ToList()));
        var location = possibleLocations[possibleLocationIndex];

        if (card.Type == CardType.AllyItem)
        {
            var possibleAllies = location.GetAllies();
            var possibleAllyIndex = await gameHub.AskFromPlayer(player, x => x.ChooseAllyToAddItemTo(possibleAllies.Select(y => y.GetState(player)).ToList()));
            var ally = possibleAllies[possibleAllyIndex];

            await player.PlayCard(gameHub, card, ally.Location);
            return;
        }

        var cardLocation = new CardLocation(location.Index, LocationType.Ally, null);
        await player.PlayCard(gameHub, card, cardLocation);
        return;
    }

    private static async Task Fate(IGameHub gameHub, Player player)
    {
        var fateablePlayers = player.Game.GetFateablePlayers(player);
        if (!fateablePlayers.Any())
            throw new Exception("There are no players to fate");
        
        var playerIndex = await gameHub.AskFromPlayer(player, x => x.ChooseFateTargetPlayer(fateablePlayers.Select(y => y.User.Id).ToList()));
        
        var targetPlayer = fateablePlayers[playerIndex];
        var fateCards = targetPlayer.DrawFateCards(2).ToList();
        var playableCards = fateCards.Where(x => targetPlayer.CanPlayFateCard(x)).ToList();
        if (!playableCards.Any())
        {
            targetPlayer.AddToFateDiscardPile(fateCards);
            return;
        }

        var playableCardIndex = await gameHub.AskFromPlayer(player, x => x.ChooseFateCard(playableCards.Select(y => y.GetState(targetPlayer)).ToList()));
        var card = playableCards[playableCardIndex];

        await PlayFateCard(gameHub, player, targetPlayer, fateCards, card);
    }

    public static async Task PlayFateCard(IGameHub gameHub, Player player, Player targetPlayer, List<Card> fateCards, Card card)
    {
        var possibleLocations = targetPlayer.Locations.Where(x => x.CanPlayFateCard(card, targetPlayer)).ToList();
        var possibleLocationIndex = await gameHub.AskFromPlayer(player, x => x.ChooseFateLocation(possibleLocations.Select(y => y.GetState(targetPlayer)).ToList()));
        var location = possibleLocations[possibleLocationIndex];

        if (card.Type == CardType.FateEffect)
        {
            await targetPlayer.PlayFateCard(gameHub, fateCards, card, null);
            return;
        }

        if (card.Type == CardType.Hero)
        {
            var cardLocation = new CardLocation(location.Index, LocationType.Hero, null);
            await targetPlayer.PlayFateCard(gameHub, fateCards, card, cardLocation);
            return;
        }

        if (card.Type == CardType.HeroItem)
        {
            var possibleHeroes = location.GetHeroes().ToList();
            var possibleHeroIndex = await gameHub.AskFromPlayer(player, x => x.ChooseHeroToAddFateItemTo(possibleHeroes.Select(y => y.GetState(targetPlayer)).ToList()));
            var hero = possibleHeroes[possibleHeroIndex];

            await targetPlayer.PlayFateCard(gameHub, fateCards, card, hero.Location);
            return;
        }

        throw new Exception();
    }

    private static async Task DiscardCards(IGameHub gameHub, Player player)
    {
        var cards = player.GetHand();
        var indexes = await gameHub.AskFromPlayer(player, x => x.ChooseCardsToDiscard(cards.Select(y => y.GetState(player)).ToList(), null, null));

        player.DiscardCardsFromHand(indexes);
    }

    private static async Task VanquishHero(IGameHub gameHub, Player player)
    {
        var defeatableHeroes = player.GetDefeatableHeroes();
        var defeatableHeroIndex = await gameHub.AskFromPlayer(player, x => x.ChooseHeroToVanquish(defeatableHeroes.Select(y => y.GetState(player)).ToList()));
        var hero = defeatableHeroes[defeatableHeroIndex];

        await VanquishHero(gameHub, player, hero);
    }

    public static async Task VanquishHero(IGameHub gameHub, Player player, Card hero)
    {
        var requiredAllyCount = player.Game.EventHandler.For<int>().Handle(player, new CalculateRequiredAllyCountEvent(hero)).DefaultIfEmpty().Max(x => x.result);
        var availableAllies = player.GetAlliesThatCanAttackAt(hero.LocationIndex!.Value).ToList();
        var availableAllyLocationIndexes = await gameHub.AskFromPlayer(player, x => x.ChooseAlliesForVanquish(availableAllies.Select(y => y.GetState(player)).ToList(), requiredAllyCount));
        var allies = availableAllyLocationIndexes.Select(x => availableAllies[x]).ToList();

        if (!Location.CanVanquishHero(hero, allies, player))
            throw new Exception("Can't vanquish");

        await player.VanquishHero(gameHub, hero, allies);
    }

    private static async Task Activate(IGameHub gameHub, Player player)
    {
        var activatableCards = player.Locations.SelectMany(x => x.GetActivatableCards(player, ActivatableMomentType.OnCardActivation)).ToList();

        var cardIndex = await gameHub.AskFromPlayer(player, x => x.ChooseCardToActivate(activatableCards.Select(y => y.GetState(player)).ToList()));
        var card = activatableCards[cardIndex];

        await card.Activate(gameHub, player, ActivatableMomentType.OnCardActivation);
    }
}