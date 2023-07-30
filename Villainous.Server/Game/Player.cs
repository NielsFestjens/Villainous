using Villainous.Server.Application.SignalR;
using static Villainous.CardType;

namespace Villainous.Server.Game;

public class Player
{
    public Player(Game game, User user, bool isOwner)
    {
        Game = game;
        User = user;
        IsOwner = isOwner;
    }

    public Game Game { get; }
    public User User { get; }
    public bool IsOwner { get; private set; }
    private string? _villainName;

    private Villain? _villain;

    private readonly List<Card> _villainDeck = new();
    private readonly List<Card> _villainDiscardPile = new();
    private readonly List<Card> _hand = new();
    private readonly List<Card> _fateDeck = new();
    private readonly List<Card> _fateDiscardPile = new();

    public int LocationIndex { get; private set; }
    private readonly List<Location> _locations = new();
    private Location CurrentLocation => _locations[LocationIndex];

    public int Power { get; private set; }
    private int _handLimit;

    private readonly List<int> _performedActions = new();
    public List<int> PerformedActions => _performedActions.ToList();
    public List<Location> Locations => _locations.ToList();
    public Location? GetLocation(Card card) => card.LocationIndex == null ? null : _locations[card.LocationIndex!.Value];

    public void MakeOwner() => IsOwner = true;

    public void ChooseVillain(string villainName)
    {
        _villainName = villainName;
    }

    public bool HasChosenVillain() => _villainName != null;
    
    public async Task Start(IGameHub gameHub, VillainLoader villainLoader, int playerIndex)
    {
        _villain = villainLoader.GetVillain(_villainName!);

        _locations.Clear();
        _locations.AddRange(_villain.CreateLocations());

        _villainDeck.Clear();
        _fateDeck.Clear();
        _villainDiscardPile.Clear();
        _hand.Clear();
        _fateDiscardPile.Clear();

        LocationIndex = 0;
        Power = playerIndex < 3 ? playerIndex : playerIndex == 3 ? 2 : 3;
        _handLimit = 4;

        _villain.CreateCards(CardTypes.VillainCardTypes).ToList().ShuffleAllTo(_villainDeck);
        _villain.CreateCards(CardTypes.FateCardTypes).ToList().ShuffleAllTo(_fateDeck);
        await DrawHandToFull(gameHub);

        VerifyConsistency();
        foreach (var otherPlayer in Game.GetFateablePlayers(this))
        {
            otherPlayer.VerifyConsistency();
        }
    }

    private void VerifyConsistency()
    {
        foreach (var location in Locations)
        {
            var cards = location.GetAllCards();
            foreach (var card in cards)
            {
                if (card.Location == null)
                    throw new Exception();
            }

            if (cards.Distinct().Count() != cards.Count)
                throw new Exception();
        }

        var allCards = Locations.SelectMany(x => x.GetAllCards())
            .Concat(_hand)
            .Concat(_villainDeck).Concat(_villainDiscardPile)
            .Concat(_fateDeck).Concat(_fateDiscardPile)
            .GroupBy(x => x.CardInfo).ToList();

        foreach (var card in allCards)
        {
            if (card.Count() != card.Key.Amount)
                throw new Exception();
        }
        
    }

    public async Task DrawHandToFull(IGameHub gameHub)
    {
        while (_hand.Count < _handLimit && (_villainDeck.Any() || _villainDiscardPile.Any()))
        {
            await DrawVillainCard(gameHub);
        }
    }

    public async Task DrawHandCards(IGameHub gameHub, int amount)
    {
        while (amount > 0 && (_villainDeck.Any() || _villainDiscardPile.Any()))
        {
            amount--;
            await DrawVillainCard(gameHub);
        }
    }

    private async Task DrawVillainCard(IGameHub gameHub)
    {
        if (!_villainDeck.Any())
            _villainDiscardPile.ShuffleAllTo(_villainDeck);

        var card = _villainDeck.MoveOneTo(_hand);
        await card.Activate(gameHub, this, ActivatableMomentType.OnReceive);
    }

    public List<Card> GetHand() => _hand.ToList();

    public List<Card> DrawFateCards(int amount)
    {
        var cards = new List<Card>();
        for (int i = 0; i < amount; i++)
        {
            if (!_fateDeck.Any())
                _fateDiscardPile.ShuffleAllTo(_fateDeck);

            if (!_fateDeck.Any())
                break;

            _fateDeck.MoveOneTo(cards);
        }

        return cards;
    }

    public void AddToTopOfFateDeck(Card fateCard)
    {
        _fateDeck.Insert(0, fateCard);
    }

    public async Task StartTurn(IGameHub gameHub)
    {
        if (Game.CurrentPlayer != this)
            throw new Exception("It's not your turn");

        if (_villain!.VillainInfo.CheckObjective(this, true))
        {
            await Game.FinishGame(gameHub, this);
            return;
        }

        await Game.CheckCondtions(gameHub);
        await InitiateSpecials(gameHub, ActivatableMomentType.BeforeVillainMove);
        Game.EventHandler.DurationEnded(Duration.UntilStartOfNextTurn);

        var needsToMove = Game.EventHandler.For<bool>().Handle(this, new CalculateNeedsToMoveEvent()).All(x => x.result);
        await MoveVillain(gameHub, this, needsToMove);
        Game.EventHandler.DurationEnded(Duration.UntilAfterVillainMovePhase);

        await Game.CheckCondtions(gameHub);

        while (true)
        {
            if (!CurrentLocation.GetActions(this).Any(x => x.IsAvailable))
                break;

            var index = await gameHub.AskFromPlayer(this, x => x.ChooseAction(CurrentLocation.GetActionStates(this), GetState()));
            if (index == null)
                break;

            var actions = CurrentLocation.GetActions(this);

            if (index >= actions.Count)
                throw new Exception("This action doesn't exist");

            var action = actions[index.Value];
            if (action.HasBeenPerformed)
                throw new Exception("You've already performed this action");

            if (action.IsCoveredByHero)
                throw new Exception("This action is currently covered by a Hero");

            if (!ActionIsPossible(action.Type))
                throw new Exception("This action is currently not possible");

            await action.Start(gameHub, this);

            _performedActions.Add(index.Value);
            await Game.CheckCondtions(gameHub);
        }

        await DrawHandToFull(gameHub);

        _performedActions.Clear();
        await Game.StartNextTurn(gameHub);
    }

    public async Task MoveVillain(IGameHub gameHub, Player currentPlayer, bool needsToMove)
    {
        var possibleLocations = Locations.Where(x => !x.IsLocked && (!needsToMove || x.Index != LocationIndex)).ToList();
        var possibleLocationIndex = await gameHub.AskFromPlayer(currentPlayer, x => x.MoveVillain(possibleLocations.Select(y => y.GetState(this)).ToList(), Game.RoundNumber));
        LocationIndex = possibleLocations[possibleLocationIndex].Index;
        await Game.EventHandler.HandleAsync(gameHub, this, new VillainMovedEvent(CurrentLocation));
    }

    private List<Card> GetAllCards() => _locations.SelectMany(x => x.GetAllCards()).ToList();
    private List<Card> GetSpecialCards(ActivatableMomentType activatableMomentType) => GetAllCards().Where(x => x.CardInfo.CanActivate(this, x, activatableMomentType)).ToList();

    private async Task InitiateSpecials(IGameHub gameHub, ActivatableMomentType activatableMomentType)
    {
        var specialCards = GetSpecialCards(activatableMomentType).ToList();
        // todo: ask which one to activate
        foreach (var card in specialCards)
        {
            await card.Activate(gameHub, this, activatableMomentType);
        }
    }

    public bool ActionIsPossible(ActionType type)
    {
        return type switch
        {
            ActionType.GainPower => true,
            ActionType.Fate => Game.GetFateablePlayers(this).Any(),
            ActionType.DiscardCards => _hand.Any(),
            ActionType.PlayCard => _hand.Any(x => CanPlayCard(x)),
            ActionType.MoveItemOrAlly => _locations.Any(x => x.GetMovableAlliesAndItems().Any()),
            ActionType.MoveHero => _locations.Any(x => x.GetMovableHeroes().Any()),
            ActionType.Vanquish => _locations.Any(x => x.GetDefeatableHeroes(this).Any()),
            ActionType.Activate => _locations.Any(x => x.GetActivatableCards(this, ActivatableMomentType.OnCardActivation).Any()),
            _ => false,
        };
    }

    public bool CanBeFated() => _fateDeck.Any() || _fateDiscardPile.Any();
    
    public async Task PerformSpecialAction(IGameHub gameHub, int locationIndex, int index)
    {
        var actions = _locations[locationIndex].GetActions(this);
        var action = actions[index];
        await action.Start(gameHub, this);
    }

    public void AddPower(int amount)
    {
        Power += amount;
    }

    public void MoveCard(Card source, int locationIndex)
    {
        var locationType = source.Location!.Type;
        var card = GetLocation(source.LocationIndex!.Value).RemoveCard(source);
        GetLocation(locationIndex).AddCard(card, new CardLocation(locationIndex, locationType, null));
    }

    public async Task PlayCard(IGameHub gameHub, Card card, CardLocation? location)
    {
        var cost = card.GetCost(this, location == null ? null : _locations[location.LocationIndex]) ?? 0;
        if (cost > Power)
            throw new Exception($"You have {Power} Power but you need {cost}");

        _hand.Remove(card);
        Power -= cost;

        if (card.Type != VillainEffect)
            GetLocation(location!.LocationIndex).AddCard(card, location);

        await card.Activate(gameHub, this, ActivatableMomentType.OnPlay);

        if (card.Type == VillainEffect)
            card.AddTo(_villainDiscardPile);

        await Game.EventHandler.HandleAsync(gameHub, this, new CardPlayedEvent(card));
    }

    public async Task PlayFateCard(IGameHub gameHub, List<Card> fateCards, Card card, CardLocation? targetCardLocation)
    {
        fateCards.Remove(card);
        fateCards.MoveAllTo(_fateDiscardPile);

        if (card.Type != FateEffect)
            GetLocation(targetCardLocation!.LocationIndex).AddCard(card, targetCardLocation);

        await card.Activate(gameHub, this, ActivatableMomentType.OnFate);

        if (card.Type == FateEffect)
            card.AddTo(_fateDiscardPile);

        Game.SetFatedPlayer(this);
        await Game.EventHandler.HandleAsync(gameHub, this, new FatedEvent(card));
    }

    public void DiscardCardsFromHand(List<int> indexes) => _hand.MoveTo(indexes, _villainDiscardPile);
    public void RemoveCardFromHand(Card card) => _hand.Remove(card);
    public void AddToVillainDiscardPile(Card card) => card.AddTo(_villainDiscardPile);

    private List<Card> GetDiscardPile(Card card) => card.LocationType == LocationType.Hero ? _fateDiscardPile : _villainDiscardPile;
    public void DiscardCardFromLocation(Card card)
    {
        GetLocation(card.Location!).RemoveCard(card).AddTo(GetDiscardPile(card));
        Game.EventHandler.CardDiscarded(card);
        foreach (var subCard in card.RemoveCards())
        {
            subCard.AddTo(GetDiscardPile(subCard));
            Game.EventHandler.CardDiscarded(card);
        }
    }


    public void DiscardCardsFromLocation(List<Card> cards)
    {
        foreach (var card in cards.OrderByDescending(x => x.LocationIndex).ThenByDescending(x => x.Location?.CardIndex).ToList())
        {
            DiscardCardFromLocation(card);
        }
    }
    
    private Location GetLocation(int locationIndex) => _locations[locationIndex];
    private Location GetLocation(CardLocation cardLocation) => GetLocation(cardLocation.LocationIndex);

    public VillainInfoDto GetVillainInfo() => _villain!.GetInfoDto();

    public List<Card> GetHeroes() => _locations.SelectMany(x => x.GetHeroes()).ToList();
    public List<Card> GetDefeatableHeroes() => _locations.SelectMany(x => x.GetDefeatableHeroes(this)).ToList();

    public List<Card> GetAlliesThatCanAttackAt(int locationIndex)
    {
        return _locations.SelectMany(x => x.GetAlliesThatCanAttackAt(locationIndex)).ToList();
    }

    public List<Card> GetMovableAlliesAndItems() => _locations.SelectMany(x => x.GetMovableAlliesAndItems()).ToList();
    public List<Card> GetAllies() => _locations.SelectMany(x => x.GetAllies()).ToList();
    public List<Card> GetMovableHeroes() => _locations.SelectMany(x => x.GetMovableHeroes()).ToList();

    public bool CanPlayCard(Card card)
    {
        return card.Type switch
        {
            Condition => false,
            VillainEffect => card.CanActivate(this, ActivatableMomentType.OnPlay),
            _ => CanAfford(card) && Locations.Any(x => x.CanPlayCard(card, this)),
        };
    }

    public bool CanAfford(Card card) => card.GetCosts(this).Any(x => x <= Power);

    public bool CanPlayFateCard(Card card) => card.Type == FateEffect || _locations.Any(x => x.CanPlayFateCard(card, this));

    public void AddToFateDiscardPile(List<Card> cards) => _fateDiscardPile.AddRange(cards);

    public async Task VanquishHero(IGameHub gameHub, Card hero, List<Card> allies)
    {
        var heroStrength = hero.GetStrength(this) ?? 0;
        var totalAllyStrength = allies.Sum(x => x.GetStrength(this) ?? 0);

        if (heroStrength > totalAllyStrength)
            throw new Exception($"Your allies are not strong enough ({totalAllyStrength}) to defeat this hero ({heroStrength})");

        DiscardCardFromLocation(hero);
        DiscardCardsFromLocation(allies);

        await Game.EventHandler.HandleAsync(gameHub, this, new HeroVanquishedEvent(hero, heroStrength));
    }

    public PlayerState GetState()
    {
        var locationState = _locations.Select(x => x.GetState(this)).ToList();
        return new PlayerState(Power, LocationIndex, _hand.Select(x => x.GetState(this)).ToList(), locationState);
    }

    public PlayerPublicState GetPublicState()
    {
        var isHandRevealed = Game.EventHandler.For<bool>().Handle(this, new CalculateAreCardsRevealedEvent()).Any(x => x.result);
        var hand = isHandRevealed ? _hand.Select(x => x.GetState(this)).ToList() : new List<CardState>();
        var locationState = _locations.Select(x => x.GetState(this)).ToList();
        return new PlayerPublicState(User.Id, Power, LocationIndex, hand, locationState);
    }
}