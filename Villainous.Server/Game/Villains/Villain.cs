namespace Villainous.Server.Game.Villains;

public class Villain
{
    private readonly VillainInfo _villainInfo;
    private readonly List<CardInfo> _cardInfos;

    public VillainInfo VillainInfo => _villainInfo;
    public int LocationLength => _villainInfo.Locations.Count;

    public Villain(VillainInfo villainInfo, List<CardInfo> cardInfos)
    {
        _villainInfo = villainInfo;
        _cardInfos = cardInfos;
    }

    public IEnumerable<Card> CreateCards(IReadOnlyCollection<CardType> types)
    {
        var relevantCardInfos = _cardInfos.Where(x => types.Contains(x.Type));
        foreach (var cardInfo in relevantCardInfos.SelectMany(x => Enumerable.Range(1, x.Amount).Select(_ => x)))
        {
            yield return new Card(cardInfo);
        }
    }
    
    public List<Location> CreateLocations()
    {
        return _villainInfo.Locations.Select((x, i) => new Location(i, x)).ToList();
    }

    public VillainInfoDto GetInfoDto() => _villainInfo.GetInfo(_cardInfos.Select(x => x.GetInfoDto()).ToList());
}