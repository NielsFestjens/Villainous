namespace Villainous.Extensions;

public static class SemaphoreSlimExtensions
{
    public static async Task Run(this SemaphoreSlim semaphore, Func<Task> action)
    {
        await semaphore.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task<T> Run<T>(this SemaphoreSlim semaphore, Func<Task<T>> action)
    {
        await semaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task Run(this SemaphoreSlim semaphore, Action action)
    {
        await semaphore.WaitAsync();
        try
        {
            action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task<T> Run<T>(this SemaphoreSlim semaphore, Func<T> action)
    {
        await semaphore.WaitAsync();
        try
        {
            return action();
        }
        finally
        {
            semaphore.Release();
        }
    }
}