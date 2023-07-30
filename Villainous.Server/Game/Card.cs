using System.Diagnostics;
using static Villainous.CardType;

namespace Villainous.Server.Game;

[DebuggerDisplay("{Type} {Name}")]
public class Card
{
    public readonly CardInfo CardInfo;
    private readonly List<Card> _cards = new();
    public CardLocation? Location { get; private set; }

    public List<Card> Cards => _cards.ToList();

    public Card(CardInfo cardInfo)
    {
        CardInfo = cardInfo;
    }

    public string Name => CardInfo.Name;
    public CardType Type => CardInfo.Type;
    public bool IsHero => CardInfo.Type == Hero;
    public bool IsAlly => CardInfo.Type == Ally;
    public LocationType LocationType => CardInfo.LocationType;
    public int? LocationIndex => Location?.LocationIndex;

    public bool CanActivate(Player player, ActivatableMomentType moment)
    {
        return player.CanAfford(this)
               && CardInfo.CanActivate(player, this, moment);
    }

    public void SetLocation(CardLocation? cardLocation)
    {
        Location = cardLocation;
    }

    public void AddCard(Card card)
    {
        _cards.Add(card);
    }

    private int? GetStrengthBonus(Player player) => (Location == null ? null : CardInfo.GetStrengthBonus(player, this)).Add(player.Game.EventHandler.For<int?>().Handle(player, new CalculateCardStrengthBonusEvent(this)).Sum(x => x.result));
    public int? GetStrength(Player player) => _cards.Aggregate(CardInfo.Strength.Add(GetStrengthBonus(player)), (current, card) => current.Add(card.GetStrength(player)));
    
    public int? GetCost(Player player, Location? location) => CardInfo.Cost.Add(player.Game.EventHandler.For<int?>().Handle(player, new CalculateCardCostBonusEvent(this, location)).Sum(x => x.result));
    public List<int?> GetCosts(Player player) => player.Locations.Select(x => GetCost(player, x)).ToList();

    public List<Card> RemoveCards() => _cards.MoveAllTo(new List<Card>());

    public CardState GetState(Player player) => new(Name, GetCosts(player), GetStrength(player), _cards.Select(x => x.GetState(player)).ToList(), Location, Type);

    public void SetLocationCardIndex(int locationCardIndex)
    {
        Location = Location! with { CardIndex = locationCardIndex };
    }

    public async Task Activate(IGameHub gameHub, Player player, ActivatableMomentType moment)
    {
        if (CardInfo.CanActivate(player, this, moment))
            await CardInfo.Activate(gameHub, player, this, moment);
    }

    public bool Is<T>() => CardInfo.GetType() == typeof(T);
}