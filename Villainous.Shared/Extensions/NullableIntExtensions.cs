namespace Villainous.Extensions;

public static class NullableIntExtensions
{
    public static int? Add(this int? a, int? b) => a ?? (b == null ? null : 0) + b ?? (a == null ? null : 0);
}