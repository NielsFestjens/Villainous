namespace Villainous.ClientCmd;

public static class LoggerExtensions
{
    public static void Print(this ILogger logger, string text)
    {
        Console.WriteLine(text);
        logger.LogDebug(text);
    }
}