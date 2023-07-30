using System.Diagnostics;
using System.Numerics;
using static Villainous.CardType;

namespace Villainous.Server.Game;

[DebuggerDisplay("{_locationInfo.Name}")]
public class Location
{
    public int Index { get; }
    private readonly LocationInfo _locationInfo;

    public bool IsLocked { get; private set; }

    public List<Card> GetActivatableCards(Player player, ActivatableMomentType moment) => GetAllCards().Where(x => x.CanActivate(player, moment)).ToList();

    private readonly List<Card> _heroLocationCards = new();
    private readonly List<Card> _allyLocationCards = new();

    public Location(int index, LocationInfo locationInfo)
    {
        Index = index;
        _locationInfo = locationInfo;
        IsLocked = locationInfo.StartsLocked;
    }

    public List<Action> GetActions(Player player)
    {
        var actions = new List<Action>();

        var index = 0;
        var heroLocationIsCovered = _heroLocationCards.Any(y => y.IsHero);
        actions.AddRange(_locationInfo.HeroLocationActions.Select(x => new Action(x, index++, player.PerformedActions, heroLocationIsCovered, player.ActionIsPossible(x.Type))));
        actions.AddRange(_locationInfo.AllyLocationActions.Select(x => new Action(x, index++, player.PerformedActions, false, player.ActionIsPossible(x.Type))));

        return actions;
    }

    private List<Card> GetLocationCardList(LocationType type) => type == LocationType.Hero ? _heroLocationCards : _allyLocationCards;

    public Card RemoveCard(Card card)
    {
        var locationCardList = GetLocationCardList(card.Location!.Type);
        card.SetLocation(null);
        locationCardList.Remove(card);
        locationCardList.ForEach((x, i) => x.SetLocationCardIndex(i));
        return card;
    }
    
    public void AddCard(Card card, CardLocation targetLocation)
    {
        var locationCardList = GetLocationCardList(targetLocation.Type);
        if (targetLocation.CardIndex != null)
        {
            var targetCard = locationCardList[targetLocation.CardIndex!.Value];
            targetCard.AddCard(card);
        }
        else
        {
            locationCardList.Add(card);
            card.SetLocation(new CardLocation(Index, card.LocationType, locationCardList.Count - 1));
        }
    }

    public LocationState GetState(Player player) => new (Index, _locationInfo.Name, GetActionStates(player), _allyLocationCards.Select(x => x.GetState(player)).ToList(), _heroLocationCards.Select(x => x.GetState(player)).ToList());
    public List<ActionState> GetActionStates(Player player) => GetActions(player).Select((x, i) => new ActionState(i, x.Type, x.IsAvailable)).ToList();

    public static bool CanVanquishHero(Card hero, List<Card> allies, Player player) => (hero.GetStrength(player) ?? 0) <= allies.Sum(y => y.GetStrength(player) ?? 0) && allies.Count >= player.Game.EventHandler.For<int>().Handle(player, new CalculateRequiredAllyCountEvent(hero)).DefaultIfEmpty().Max(x => x.result);
    public List<Card> GetDefeatableHeroes(Player player) => GetHeroes().Where(x => CanVanquishHero(x, player.GetAlliesThatCanAttackAt(Index), player)).ToList();
    public List<Card> GetAlliesThatCanAttackAt(int locationIndex) => _allyLocationCards.Where(x => x.IsAlly && locationIndex == Index).ToList();

    public bool HasCardOfType(CardType type) => _allyLocationCards.Any(x => x.Type == type) || _heroLocationCards.Any(x => x.Type == type);

    public List<Card> GetHeroes() => _heroLocationCards.Where(x => x.IsHero).ToList();

    public List<Card> GetAllyCards(CardType type) => _allyLocationCards.Where(x => x.Type == type).ToList();

    public List<Card> GetAllCards() => _heroLocationCards.Concat(_allyLocationCards).ToList();

    public List<Card> GetMovableAlliesAndItems() => _allyLocationCards.Where(x => x.Type is Ally or VillainItem or VillainSpecial).ToList();
    public List<Card> GetAllies() => _allyLocationCards.Where(x => x.Type is Ally).ToList();
    public List<Card> GetMovableHeroes() => _heroLocationCards.Where(x => x.Type is Hero).ToList();
    
    public bool CanPlayCard(Card card, Player player) => !IsLocked && (card.Type != AllyItem || GetAllies().Any()) && (player.Game.EventHandler.For<bool>().Handle(player, new CalculateCanPlayCardEvent(card, this))).All(x => x.result);
    public bool CanPlayFateCard(Card card, Player player) => !IsLocked && (card.Type != HeroItem || GetHeroes().Any()) && (player.Game.EventHandler.For<bool>().Handle(player, new CalculateCanFateEvent(card, new CardLocation(Index, LocationType.Hero, null)))).All(x => x.result);
}