namespace Villainous.ClientCmd;

public class LobbyClient : ILobbyClient
{
    private readonly VillainousClient _villainousClient;
    private readonly ILogger _logger;

    public readonly Dictionary<Guid, User> LobbyUsers = new();
    public string GetName(Guid id) => id == _villainousClient.UserId ? "You" : LobbyUsers[id].Username;

    public readonly SemaphoreSlim GameIdLock = new(1, 1);
    public readonly List<Guid> GameIds = new();

    public LobbyClient(VillainousClient villainousClient, ILogger logger)
    {
        _villainousClient = villainousClient;
        _logger = logger;
    }

    public async Task JoinLobby()
    {
        _logger.Print("Joining the lobby");
        await _villainousClient.Connection.JoinLobby();
    }

    public async Task PlayerJoinedLobby(Guid userId, string name, bool isAvailable)
    {
        if (LobbyUsers.ContainsKey(userId))
        {
            LobbyUsers[userId].IsAvailable = isAvailable;
            return;
        }

        LobbyUsers[userId] = new User { Id = userId, Username = name, IsAvailable = isAvailable };
        _logger.Print($"{GetName(userId)} joined the lobby");

        if (userId == _villainousClient.UserId && _villainousClient.IsFirstUser)
        {
            _logger.Print("Creating a game");
            await _villainousClient.Connection.CreateGame();
        }

        await Task.CompletedTask;
    }

    public async Task GameCreated(Guid gameId)
    {
        var isNewGame = await GameIdLock.Run(() =>
        {
            if (GameIds.Contains(gameId))
                return false;

            GameIds.Add(gameId);
            return true;
        });

        if (!isNewGame || GameIds.Count > 1)
            return;

        if (_villainousClient.IsFirstUser)
        {
            _logger.Print("Your game is created");
        }
        else
        {
            _logger.Print("A game was created, do you want to join it?");

            _logger.Print("Joining game");
            await _villainousClient.Connection.JoinGame(gameId);
        }
    }

    public async Task PlayerIsPlayingGame(Guid userId)
    {
        LobbyUsers[userId].IsAvailable = false;
        await Task.CompletedTask;
    }

    public async Task PlayerStoppedPlayingGame(Guid userId)
    {
        LobbyUsers[userId].IsAvailable = true;
        await Task.CompletedTask;
    }
}