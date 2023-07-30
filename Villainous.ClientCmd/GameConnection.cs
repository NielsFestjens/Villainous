using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.SignalR.Client;

namespace Villainous.ClientCmd;

public class GameConnection : IGameServer
{
    private readonly HubConnection _connection;

    public GameConnection(HubConnection connection)
    {
        _connection = connection;
    }
    
    public static async Task<GameConnection> ConfigureConnection(VillainousClient client, VillainousClientConfig config, LoginInfo loginInfo, ILogger logger)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{config.BaseAddress}/gameHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(loginInfo.AccessToken)!;
                options.CloseTimeout = TimeSpan.FromMinutes(5);
            })
            .Build();

        connection.ServerTimeout = TimeSpan.FromMinutes(5);

        connection.Closed += exception => { Console.WriteLine($"Connection closed: {exception}"); return Task.CompletedTask; };
        connection.Reconnecting += exception => { Console.WriteLine($"Reconnecting: {exception}"); return Task.CompletedTask; };
        connection.Reconnected += param => { Console.WriteLine($"Reconnected: {param}"); return Task.CompletedTask; };

        Autowire<IVillainousClient>(client, connection, logger);
        Autowire<ILobbyClient>(client.Lobby, connection, logger);

        await connection.StartAsync();
        logger.LogDebug($"ConnectionId: {connection.ConnectionId}");

        return new GameConnection(connection);
    }

    private static void Autowire<TClient>(TClient client, HubConnection connection, ILogger logger)
    {
        var clientMethods = typeof(TClient).GetAllMethods(BindingFlags.Instance | BindingFlags.Public).ToList();
        var registerOnWithResultMethod = typeof(GameConnection).GetMethod(nameof(RegisterOnWithResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var clientMethod in clientMethods)
        {
            var parameterTypes = clientMethod.GetParameters().Select(x => x.ParameterType).ToArray();
            var returnType = clientMethod.ReturnType;
            var method = typeof(TClient).GetMethod(clientMethod.Name, BindingFlags.Public | BindingFlags.Instance)!;
            if (returnType == typeof(Task))
            {
                connection.On(clientMethod.Name, parameterTypes, parameters => Handle(logger, async () => await (Task)method.Invoke(client, parameters)!));
            }
            else
            {
                var invokeHandleWithResult = registerOnWithResultMethod.MakeGenericMethod(returnType.GetGenericArguments().First());
                invokeHandleWithResult.Invoke(null, new object?[] { connection, logger, clientMethod, client });
            }
        }
    }

    [DebuggerStepThrough]
    private static async Task Handle(ILogger logger, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, null);
            throw;
        }
    }

    [DebuggerStepThrough]
    private static void RegisterOnWithResult<T>(HubConnection connection, ILogger logger, MethodInfo clientMethod, object client)
    {
        var parameterTypes = clientMethod.GetParameters().Select(x => x.ParameterType).ToArray();

        [DebuggerStepThrough]
        async Task<T> Handler(object?[] parameters)
        {
            [DebuggerStepThrough]
            async Task<T> Action() => await (Task<T>)clientMethod.Invoke(client, parameters)!;

            return await HandleWithResult(logger, Action);
        }

        connection.On(clientMethod.Name, parameterTypes, Handler);
    }

    [DebuggerStepThrough]
    private static async Task<T> HandleWithResult<T>(ILogger logger, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, null);
            throw;
        }
    }
    
    public async Task JoinLobby() => await _connection.InvokeAsync(nameof(IGameServer.JoinLobby));
    public async Task CreateGame() => await _connection.InvokeAsync(nameof(IGameServer.CreateGame));
    public async Task TriggerLobby() => await _connection.InvokeAsync(nameof(IGameServer.TriggerLobby));
    public async Task JoinGame(Guid id) => await _connection.InvokeAsync(nameof(IGameServer.JoinGame), id);
    public async Task ChooseVillain(string villainName) => await _connection.InvokeAsync(nameof(IGameServer.ChooseVillain), villainName);
    public async Task StartGame() => await _connection.InvokeAsync(nameof(IGameServer.StartGame));
    public async Task StartTurn() => await _connection.InvokeAsync(nameof(IGameServer.StartTurn));
    public async Task LeaveGame() => await _connection.InvokeAsync(nameof(IGameServer.LeaveGame));
}