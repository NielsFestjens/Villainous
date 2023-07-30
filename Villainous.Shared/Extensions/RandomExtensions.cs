namespace Villainous.Extensions;

public static class RandomExtensions
{
    public static T Choose<T>(this Random random, List<T> items) => items[Random.Shared.Next(items.Count)];
}