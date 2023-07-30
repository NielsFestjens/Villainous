namespace Villainous.Extensions;

public static class EnumerableExtensions
{
    public static string ToNiceString<T>(this IEnumerable<T> items, string separator = ", ") => string.Join(separator, items);

    public static List<T> ForEach<T>(this IEnumerable<T> items, Action<T, int> action)
    {
        var itemsList = items.ToList();
        for (var i = 0; i < itemsList.Count(); i++)
        {
            action(itemsList[i], i);
        }

        return itemsList;
    }

    public static IAsyncEnumerable<TResult> SelectAsync<T, TResult>(this IEnumerable<T> enumerable, Func<T, ValueTask<TResult>> action)
    {
        return enumerable.ToAsyncEnumerable().SelectAwait(action);
    }

    public static async ValueTask<List<TResult>> SelectListAsync<T, TResult>(this IEnumerable<T> enumerable, Func<T, ValueTask<TResult>> action)
    {
        return await enumerable.SelectAsync(action).ToListAsync();
    }

    public static async ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, ValueTask<TAccumulate>> accumulator)
    {
        return await source.ToAsyncEnumerable().AggregateAwaitAsync(seed, accumulator);
    }
}