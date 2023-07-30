using System.Reflection;
using Villainous.Server.Game.Villains.Editions.TheWorstTakesItAll.Maleficent;

namespace Villainous.Server.Game.Villains;

public class VillainInfoLoader
{
    private readonly Lazy<List<VillainInfo>> _villainInfos = new(() => LoadAllOfType<VillainInfo>(typeof(MaleficentInfo).Assembly));
    private readonly Lazy<List<CardInfo>> _cardInfos = new(() => LoadAllOfType<CardInfo>(typeof(MaleficentInfo).Assembly));

    public List<VillainInfo> VillainInfos => _villainInfos.Value.ToList();
    public List<CardInfo> CardInfos => _cardInfos.Value.ToList();
    
    public static List<T> LoadAllOfType<T>(Assembly assembly)
    {
        var type = typeof(T);
        var types = assembly.GetTypes().Where(x => x.IsAssignableTo(type) && !x.IsAbstract);
        return types.Select(x => (T)Activator.CreateInstance(x)!).ToList();
    }
}