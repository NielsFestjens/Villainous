namespace Villainous.Server.Game;

public class Game
{
    private readonly VillainLoader _villainLoader;

    public Guid Id { get; }

    private GameStatus _status;
    private readonly List<Player> _players = new();

    public int RoundNumber { get; private set; }
    private int _currentPlayerIndex;

    public Player CurrentPlayer => _players[_currentPlayerIndex];
    public List<User> Users => _players.Select(x => x.User).ToList();
    
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Player? _fatedPlayer;

    public EventHandler EventHandler = new();

    private readonly List<(Player player, CardInfo card)> _checkedConditions = new();

    public Game(VillainLoader villainLoader, User owner)
    {
        _villainLoader = villainLoader;
        Id = Guid.NewGuid();
        _status = GameStatus.Created;
        _players.Add(new Player(this, owner, true));
    }

    public void AddPlayer(User user)
    {
        if (_players.Any(x => x.User == user))
            throw new Exception("This user has already joined");

        _players.Add(new Player(this, user, false));
    }
    
    public Player GetPlayer(User user) => _players.Single(x => x.User == user);

    public void RemovePlayer(User user)
    {
        lock (_players)
        {
            var player = _players.SingleOrDefault(x => x.User == user);
            if (player == null)
                return;
            
            _players.Remove(player);
            if (!_players.Any())
            {
                _status = GameStatus.Abandoned;
                return;
            }

            if (player.IsOwner)
            {
                _players[0].MakeOwner();
            }
        }
    }

    public async Task ChooseVillain(IGameHub gameHub, Player player, string villainName)
    {
        gameHub.WriteLog($"ChooseVillain {villainName}");

        await _lock.Run(async () =>
        {
            player.ChooseVillain(villainName);

            if (IsGameReadyToStart())
                await gameHub.SendToGame($"{nameof(IGameClient.GameReadyToStart)}({_players.Count})", x => x.GameReadyToStart(_players.Count));
        });
    }

    public async Task Start(IGameHub gameHub)
    {
        gameHub.WriteLog("StartGame");

        if (_players.Any(x => !x.HasChosenVillain()))
            throw new Exception("Not everybody has chosen a villain");

        if (_status != GameStatus.Created)
            throw new Exception("The game cannot be started anymore");

        _status = GameStatus.Started;
        RoundNumber = 0;
        _players.ShuffleInPlace();
        _currentPlayerIndex = 0;

        await _players.ToAsyncEnumerable().ForEachAwaitAsync(async (x, i) => await x.Start(gameHub, _villainLoader, i));
        var playerInfos = _players.Select((x, i) => new PlayerInfoDto(i, x.User.Id, x.GetVillainInfo())).ToList();

        await gameHub.SendToGame(nameof(IGameClient.GameStarted), x => x.GameStarted(playerInfos));

        await StartTurn(gameHub);
    }

    private async Task StartTurn(IGameHub gameHub)
    {
        _checkedConditions.Clear();

        foreach (var player in _players)
        {
            await gameHub.SendToPlayer(player, nameof(IGameClient.TurnStarted), x => x.TurnStarted(CurrentPlayer.User.Id, GetState(player)));
        }

#pragma warning disable CS4014
        gameHub.SendToPlayer(CurrentPlayer, nameof(IGameClient.StartTurnRequested), x => x.StartTurnRequested());
#pragma warning restore CS4014
    }

    public void SetFatedPlayer(Player player) => _fatedPlayer = player;

    public bool CanFatePlayer(Player player) => player.CanBeFated() && (_players.Count <= 2 || _fatedPlayer != player);

    public async Task StartNextTurn(IGameHub gameHub)
    {
        _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        if (_currentPlayerIndex == 0)
            RoundNumber++;

        await StartTurn(gameHub);
    }

    public async Task FinishGame(IGameHub gameHub, Player winningPlayer)
    {
        _status = GameStatus.Finished;
        _fatedPlayer = null;
        await gameHub.SendToGame(nameof(IGameClient.PlayerWonGame), x => x.PlayerWonGame(winningPlayer.User.Id));
    }

    public enum GameStatus
    {
        Created = 1,
        Started = 2,
        Finished = 3,
        Abandoned = 4,
    }

    public List<Player> GetFateablePlayers(Player fatingPlayer) => _players.Where(x => x != fatingPlayer && CanFatePlayer(x)).ToList();

    public bool IsGameReadyToStart() => _players.Count > 1 && _players.All(x => x.HasChosenVillain());

    public async Task CheckCondtions(IGameHub gameHub)
    {
        var currentPlayer = CurrentPlayer;
        foreach (var otherPlayer in _players.Where(x => x != currentPlayer))
        {
            var cards = otherPlayer.GetHand().Where(x => x.CardInfo.CanActivate(currentPlayer, x, ActivatableMomentType.OnCondition)).ToList();
            foreach (var card in cards)
            {
                if (_checkedConditions.Contains((otherPlayer, card.CardInfo)))
                    continue;
                
                _checkedConditions.Add((otherPlayer, card.CardInfo));
                if (!await gameHub.AskFromPlayer(otherPlayer, x => x.ActivateCondition(card.GetState(otherPlayer))))
                    continue;

                otherPlayer.RemoveCardFromHand(card);
                await card.Activate(gameHub, otherPlayer, ActivatableMomentType.OnCondition);
                otherPlayer.AddToVillainDiscardPile(card);
            }
        }
    }

    public GameState GetState(Player player)
    {
        var otherPlayers = _players.Where(x => x != player).Select(x => x.GetPublicState()).ToList();
        return new GameState(player.GetState(), otherPlayers);
    }
}