namespace Villainous.Extensions;

public static class ListExtensions
{
    public static T MoveOneTo<T>(this List<T> source, int index, List<T> target)
    {
        return source.RemoveAndGetAt(index).AddTo(target);
    }

    public static T MoveOneTo<T>(this List<T> source, T item, List<T> target)
    {
        return source.RemoveAndGetAt(item).AddTo(target);
    }

    public static T MoveOneTo<T>(this List<T> source, List<T> target)
    {
        return source.MoveOneTo(0, target);
    }

    public static List<T> MoveTo<T>(this List<T> source, List<int> indexes, List<T> target)
    {
        foreach (var index in indexes.OrderByDescending(x => x))
        {
            source.MoveOneTo(index, target);
        }
        return target;
    }

    public static T RemoveAndGetAt<T>(this List<T> source, int index)
    {
        var item = source[index];
        source.RemoveAt(index);
        return item;
    }

    public static T RemoveAndGetAt<T>(this List<T> source, T item)
    {
        source.Remove(item);
        return item;
    }

    public static T AddTo<T>(this T item, List<T> target)
    {
        target.Add(item);
        return item;
    }

    public static void ShuffleAllTo<T>(this List<T> source, List<T> target)
    {
        source.MoveAllTo(new List<T>()).Shuffle().MoveAllTo(target);
    }

    public static List<T> MoveAllTo<T>(this List<T> source, List<T> target)
    {
        target.AddRange(source);
        source.Clear();
        return target;
    }

    public static void ShuffleInPlace<T>(this IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var k = Random.Shared.Next(i + 1);
            (values[k], values[i]) = (values[i], values[k]);
        }
    }

    public static List<T> Shuffle<T>(this IList<T> values)
    {
        return !values.Any() ? values.ToList() : Enumerable.Range(0, values.Count).ToDictionary(x => x, x => Random.Shared.NextDouble()).OrderBy(x => x.Value).Select(x => values[x.Key]).ToList();
    }
}