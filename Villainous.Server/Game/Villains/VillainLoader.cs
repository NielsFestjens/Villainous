namespace Villainous.Server.Game.Villains;

public class VillainLoader
{
    private readonly VillainInfoLoader _villainInfoLoader;

    public VillainLoader(VillainInfoLoader villainInfoLoader)
    {
        _villainInfoLoader = villainInfoLoader;
    }

    public Villain GetVillain(string villainName)
    {
        var villainInfo = _villainInfoLoader.VillainInfos.Single(x => x.Name == villainName);
        var cardInfos = _villainInfoLoader.CardInfos.Where(x => x.VillainName == villainName).ToList();

        return new Villain(villainInfo, cardInfos);
    }
}