namespace Villainous.Server.Game;

public class Lobby
{
    private readonly VillainLoader _villainLoader;

    private readonly List<User> _lobbyUsers = new();
    private static readonly SemaphoreSlim LobbyLock = new(1, 1);
    public List<User> LobbyUsers => _lobbyUsers.ToList();

    private readonly List<Game> _games = new();
    public Game GetGame(Guid id) => _games.Single(x => x.Id == id);
    
    public Lobby(VillainLoader villainLoader)
    {
        _villainLoader = villainLoader;
    }

    public async Task JoinLobby(IGameHub gameHub)
    {
        gameHub.WriteLog("JoinLobby");

        var user = gameHub.GetUser();
        var connectionId = gameHub.GetConnectionId();
        await LobbyLock.Run(() =>
        {
            if (_lobbyUsers.Any(x => x.ConnectionId == connectionId))
                throw new Exception("Double connection id!");
            user.ConnectionId = connectionId;

            if (!_lobbyUsers.Contains(user))
                _lobbyUsers.Add(user);
        });

        await gameHub.SendToLobby($"{nameof(IGameClient.PlayerJoinedLobby)}({user.Id}, {user.DisplayName})", x => x.PlayerJoinedLobby(user.Id, user.DisplayName, user.GameId == null));
        await TriggerLobby(gameHub);
    }

    public async Task TriggerLobby(IGameHub gameHub)
    {
        var gameIds = _games.Select(x => x.Id).ToList();
        var lobbyUsers = _lobbyUsers.Where(x => x.Id != gameHub.GetUserId()).Select(x => new { x.Id, x.DisplayName, IsAvailable = x.GameId == null }).ToList();
        gameHub.WriteLog($"TriggerLobby, we have [{gameIds.ToNiceString()}] and [{lobbyUsers.ToNiceString()}]");
        foreach (var user in lobbyUsers)
        {
            await gameHub.SendToCaller($"{nameof(IGameClient.PlayerJoinedLobby)}({user.Id}, {user.DisplayName})", x => x.PlayerJoinedLobby(user.Id, user.DisplayName, user.IsAvailable));
        }
        foreach (var gameId in gameIds)
        {
            await gameHub.SendToCaller($"{nameof(IGameClient.GameCreated)}({gameId})", x => x.GameCreated(gameId));
        }
    }

    public async Task LeaveGame(IGameHub gameHub)
    {
        var user = gameHub.GetUser();
        if (user.GameId == null)
            return;

        var game = GetGame(user.GameId.Value);
        await gameHub.SendToLobby($"{nameof(IGameClient.PlayerStoppedPlayingGame)}({user.Id})", x => x.PlayerStoppedPlayingGame(user.Id));
        await gameHub.SendToGame($"{nameof(IGameClient.PlayerLeftGame)}({user.Id})", x => x.PlayerLeftGame(user.Id));
        game.RemovePlayer(user);
        user.GameId = null;
    }

    public async Task CreateGame(IGameHub gameHub)
    {
        gameHub.WriteLog("CreateGame");

        await LeaveGame(gameHub);

        var owner = gameHub.GetUser();
        var game = new Game(_villainLoader, owner);
        _games.Add(game);
        owner.GameId = game.Id;

        await gameHub.SendToLobby($"{nameof(IGameClient.PlayerIsPlayingGame)}({owner.Id})", x => x.PlayerIsPlayingGame(owner.Id));
        await gameHub.SendToLobby($"{nameof(IGameClient.GameCreated)}({game.Id})", x => x.GameCreated(game.Id));
        await gameHub.SendToGame($"{nameof(IGameClient.PlayerJoinedGame)}({owner.Id})", x => x.PlayerJoinedGame(owner.Id));
    }

    public async Task JoinGame(IGameHub gameHub, Guid gameId)
    {
        gameHub.WriteLog("JoinGame");
        var user = gameHub.GetUser();

        await LeaveGame(gameHub);
        GetGame(gameId).AddPlayer(user);
        user.GameId = gameId;

        await gameHub.SendToLobby($"{nameof(IGameClient.PlayerIsPlayingGame)}({user.Id})", x => x.PlayerIsPlayingGame(user.Id));
        await gameHub.SendToGame($"{nameof(IGameClient.PlayerJoinedGame)}({user.Id})", x => x.PlayerJoinedGame(user.Id));
    }
}