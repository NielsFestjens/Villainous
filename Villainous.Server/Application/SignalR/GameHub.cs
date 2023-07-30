using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace Villainous.Server.Application.SignalR;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class GameHub : BaseHub<IGameClient>, IGameHub, IGameServer
{
    private readonly Lobby _lobby;

    public GameHub(Lobby lobby, UserContext userContext, ILogger logger) : base(userContext, logger)
    {
        _lobby = lobby;
    }

    public async Task JoinLobby() => await _lobby.JoinLobby(this);
    public async Task CreateGame() => await _lobby.CreateGame(this);
    public async Task TriggerLobby() => await _lobby.TriggerLobby(this);
    public async Task JoinGame(Guid id) => await _lobby.JoinGame(this, id);
    public async Task LeaveGame() => await _lobby.LeaveGame(this);

    public async Task ChooseVillain(string villainName) => await GetGame().ChooseVillain(this, GetPlayer(), villainName);
    public async Task StartGame() => await GetGame().Start(this);
    public async Task StartTurn() => await GetPlayer().StartTurn(this);

    public async Task SendToLobby(string log, Func<IGameClient, Task> action) => await SendToUsers(_lobby.LobbyUsers, $"{log} for Lobby", action);
    public async Task SendToGame(string log, Func<IGameClient, Task> action) => await SendToUsers(GetGame().Users, $"{log} for Game", action);
    public async Task SendToPlayer(Player player, string log, Func<IGameClient, Task> action) => await SendToUser(player.User, log, action);
    
    private Player GetPlayer() => GetGame().GetPlayer(GetUser());
    public Game.Game GetGame() => _lobby.GetGame(GetUser().GameId!.Value);
}

public static class GameHubExtensions
{
    [DebuggerStepThrough]
    public static async Task<TResult> AskFromPlayer<TResult>(this IGameHub gameHub, Player player, Func<IGameClient, Task<TResult>> action) => await gameHub.AskFromUser(player.User, action);
}