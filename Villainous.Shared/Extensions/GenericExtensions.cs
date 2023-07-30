namespace Villainous.Extensions;

public static class GenericExtensions
{
    public static TResult Map<TInput, TResult>(this TInput input, Func<TInput, TResult> map) => map(input);
    public static TInput Do<TInput>(this TInput input, Action<TInput> map) { map(input); return input; }
}