using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace Villainous.Server.Application.SignalR;

public interface IHub<out T>
{
    string GetConnectionId();
    Guid GetUserId();
    User GetUser();
    Task SendToCaller(string log, Func<T, Task> action);
    void WriteLog(string message);
}

public abstract class BaseHub<TClient> : Hub<TClient>, IHub<TClient> where TClient : class
{
    private readonly UserContext _context;
    private readonly ILogger _logger;

    protected BaseHub(UserContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        WriteLog($"Connected on {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        WriteLog($"Disconnected on {Context.ConnectionId}: {exception}");
        return base.OnDisconnectedAsync(exception);
    }

    public string GetConnectionId() => Context.ConnectionId;
    public Guid GetUserId() => Guid.Parse(Context.User!.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value);
    public User GetUser() => _context.GetUser(GetUserId());
    
    public async Task SendToCaller(string log, Func<TClient, Task> action)
    {
        WriteLog($"{log} to self");
        await action(Clients.Clients(Context.ConnectionId));
    }

    public async Task SendToUsers(IReadOnlyCollection<User> users, string log, Func<TClient, Task> action)
    {
        WriteLog($"{log} to [{users.Select(x => x.DisplayName).ToNiceString()}]");
        await action(Clients.Clients(users.Select(x => x.ConnectionId)));
    }

    public async Task SendToUser(User user, string log, Func<TClient, Task> action)
    {
        WriteLog($"{log} to {user.DisplayName}");
        await action(Clients.Client(user.ConnectionId));
    }

    public void WriteLog(string message) => _logger.LogDebug($"{GetUser().DisplayName}: {message}");
}

public static class HubExtensions
{
    [DebuggerStepThrough]
    public static async Task<TResult> AskFromUser<TClient, TResult>(this IHub<TClient> hub, User user, Func<TClient, Task<TResult>> action) where TClient : class
    {
        return await action(((BaseHub<TClient>)hub).Clients.Client(user.ConnectionId));
    }
}