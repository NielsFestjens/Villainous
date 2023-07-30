namespace Villainous.Server.Game;

public interface IGameHub : IHub<IGameClient>
{
    Task SendToLobby(string log, Func<IGameClient, Task> action);
    Task SendToGame(string log, Func<IGameClient, Task> action);
    Task SendToPlayer(Player player, string log, Func<IGameClient, Task> action);
}