namespace Villainous.ClientCmd;

public class VillainousClient : IVillainousClient
{
    private readonly ILogger _logger;
    public LobbyClient Lobby { get; }

    private VillainousClientConfig _config = null!;
    private LoginInfo _loginInfo = null!;
    public GameConnection Connection = null!;
    private readonly Game _game = new();

    public bool IsFirstUser => _config.IsFirstUser;
    public Guid UserId => _loginInfo.Id;

    private string GetUsername(Guid id) => Lobby.LobbyUsers[id].Username;

    public VillainousClient(ILogger logger)
    {
        _logger = logger;
        Lobby = new LobbyClient(this, _logger);
    }

    public async Task Start(VillainousClientConfig config)
    {
        _config = config;
        _loginInfo = await ClientRegistration.Register(_logger, config);
        Connection = await GameConnection.ConfigureConnection(this, config, _loginInfo, _logger);
        await Lobby.JoinLobby();
    }

    public async Task PlayerJoinedGame(Guid id)
    {
        if (id != _loginInfo.Id)
        {
            _logger.Print($"{GetUsername(id)} joined the game");
            return;
        }

        _logger.Print("You joined the game. Choose a villain");
        _game.VillainName = "Maleficent";

        _logger.Print($"Choosing {_game.VillainName}");
        await Connection.ChooseVillain(_game.VillainName);
    }

    public async Task PlayerLeftGame(Guid id)
    {
        _logger.Print($"{GetUsername(id)} left the game. Sadsies :'(");
        await Task.CompletedTask;
    }

    public async Task GameReadyToStart(int amountOfPlayers)
    {
        _logger.Print($"The game is ready to start, there are {amountOfPlayers} players");

        if (_config.IsFirstUser)
        {
            _logger.Print("Starting the game");
            await Connection.StartGame();
        }
    }

    public async Task GameStarted(List<PlayerInfoDto> playerInfos)
    {
        _game.Players = playerInfos;
        _game.VillainInfo = playerInfos.Single(x => x.UserId == _loginInfo.Id).VillainInfo;

        var stringBuilder = new StringBuilder();
        stringBuilder.Append("The game has started, these are the players with their villains:\r\n");
        foreach (var player in playerInfos)
        {
            stringBuilder.Append($"* {GetUsername(player.UserId)}: {player.VillainInfo.Name} (from {player.VillainInfo.Edition})\r\n");

            stringBuilder.Append("* * Locations:\r\n");
            foreach (var location in player.VillainInfo.Locations)
            {
                stringBuilder.Append($"* * * {location.Name}\r\n");
                stringBuilder.Append($"* * * * Hero Location Actions: {location.HeroLocationActions.Select(action => $"{action.Type}{(action.Amount != null ? $" {action.Amount}" : "")}").ToNiceString()}\r\n");
                stringBuilder.Append($"* * * * Ally Location Actions: {location.AllyLocationActions.Select(action => $"{action.Type}{(action.Amount != null ? $" {action.Amount}" : "")}").ToNiceString()}\r\n");
            }

            stringBuilder.Append("* * Villain Cards:\r\n");
            foreach (var card in player.VillainInfo.Cards.Where(x => CardTypes.VillainCardTypes.Contains(x.Type)))
            {
                stringBuilder.Append($"* * * {card.Amount} x {card.Name} ({card.Type}{(card.Cost == null ? "" : $", {card.Cost} Power")}{(card.Strength == null ? "" : $", {card.Strength} Strength")}): {card.Description.Replace("\r\n", " ")}\r\n");
            }

            stringBuilder.Append("* * Fate Cards:\r\n");
            foreach (var card in player.VillainInfo.Cards.Where(x => CardTypes.FateCardTypes.Contains(x.Type)))
            {
                stringBuilder.Append($"* * * {card.Amount} x {card.Name} ({card.Type}{(card.Cost == null ? "" : $", {card.Cost} Power")}{(card.Strength == null ? "" : $", {card.Strength} Strength")}): {card.Description.Replace("\r\n", " ")}\r\n");
            }

        }

        _logger.Print(stringBuilder.ToString());

        await Task.CompletedTask;
    }

    public async Task TurnStarted(Guid userId, GameState gameState)
    {
        _logger.Print($"Turn started for {GetUsername(userId)}.");
        UpdateState(gameState.Player);

        if (userId != _loginInfo.Id)
            _logger.Print($"Waiting for {GetUsername(userId)} to make their move.");

        await Task.CompletedTask;
    }

    private void UpdateState(PlayerState playerState)
    {
        var locationInfo = _game.VillainInfo.Locations[playerState.VillainLocationIndex];
        _logger.Print($@"You have {playerState.Power} Power.
* You are at {locationInfo.Name} (position index {playerState.VillainLocationIndex})).
* Your hand cards are:
* * {playerState.Hand.Select(x => $"{x.Name} (cost: {SummarizeCosts(x.Cost)}{(x.Strength != null ? $", strength: {x.Strength}" : "")})").ToNiceString("\r\n* * ")}");
        _game.PlayerState = playerState;
        _game.VillainLocationIndex = playerState.VillainLocationIndex;
    }

    private static string SummarizeCosts(IEnumerable<int?> costs)
    {
        var distinctCosts = costs.OfType<int>().Distinct().OrderBy(x => x).ToList();
        return distinctCosts.Count switch { 0 => "", 1 => distinctCosts[0].ToString(), _ => $"{distinctCosts[0]}-{distinctCosts.Last()}" };
    }

    public async Task StartTurnRequested()
    {
        _logger.Print("Start turn requested");
        _logger.Print("Starting turn");

#pragma warning disable CS4014
        Connection.StartTurn();
#pragma warning restore CS4014

        await Task.CompletedTask;
    }

    public async Task<int> MoveVillain(List<LocationState> locations, int roundNumber)
    {
        _logger.Print($"It's your turn (round {roundNumber}). Move your villain. You are at position {_game.VillainLocationIndex}.");

        var index = locations.Select((location, i) => (location, i)).FirstOrDefault(x => x.location.Index > _game.VillainLocationIndex).i;
        _game.VillainLocationIndex = locations[index].Index;
        _logger.Print($"Moving to {_game.VillainInfo.Locations[_game.VillainLocationIndex].Name}(position index {_game.VillainLocationIndex})");
        return await Task.FromResult(index);
    }

    public async Task<int?> ChooseAction(List<ActionState> actions, PlayerState playerState)
    {
        UpdateState(playerState);
        _logger.Print($"Choose an action. Actions: [{actions.Select(x => $"[{x.Index}] {x.Type}{(x.IsAvailable ? "" : " (not available)")}").ToNiceString()}]");

        var chosenAction = actions.FirstOrDefault(x => x.IsAvailable);
        _logger.Print(chosenAction == null ? "Stopping with actions" : $"Choosing action {chosenAction.Index}: {chosenAction.Type}");

        return await Task.FromResult(chosenAction?.Index);
    }

    public async Task<int> ChooseItemOrAllyToMove(List<CardState> cards)
    {
        _logger.Print($"Choose the ally or item that you want to move: {cards.Select(x => x.Name).ToNiceString()}");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseLocationToMoveItemOrAllyTo(List<LocationState> locations)
    {
        _logger.Print("Choose a location to where you want to move the ally or item");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseHeroToMove(List<CardState> cards)
    {
        _logger.Print($"Choose the hero that you want to move: {cards.Select(x => x.Name).ToNiceString()}");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseLocationToMoveHeroTo(List<LocationState> locations)
    {
        _logger.Print("Choose a location to where you want to move the hero");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseCardToPlay(List<CardState> cards)
    {
        _logger.Print($"Choose which card you want to play. Cards: {cards.Select(x => x.Name).ToNiceString()}");
        var chosenCardIndex = 0;
        _logger.Print($"Choosing card {cards[chosenCardIndex].Name} (index {chosenCardIndex})");

        return await Task.FromResult(chosenCardIndex);
    }

    public async Task<int> ChooseLocationToPlayCardTo(List<LocationState> locations)
    {
        _logger.Print($"Choose where you want to put the card: {locations.Select(x => x.Name).ToNiceString()}");

        var locationIndex = GetLocationToPlayCardTo(locations);
        _logger.Print($"Choosing location {locations[locationIndex].Name}");
        return await Task.FromResult(locationIndex);
    }

    private int GetLocationToPlayCardTo(List<LocationState> locations)
    {
        if (_game.IsVillain(VillainNames.Maleficent))
        {
            for (var i = 0; i < locations.Count; i++)
            {
                if (!locations[i].AllyCards.Any(x => x.Type == CardType.VillainSpecial))
                    return i;
            }
        }

        return 0;
    }

    public async Task<int> ChooseAllyToAddItemTo(List<CardState> allies)
    {
        _logger.Print($"Choose the ally to whom you want to add the item: {allies.Select(x => x.Name).ToNiceString()}");

        var allyIndex = 0;
        _logger.Print($"Choosing ally {allies[allyIndex]}");
        return await Task.FromResult(allyIndex);
    }

    public async Task<int> ChooseFateTargetPlayer(List<Guid> fateablePlayers)
    {
        _logger.Print("Choose a player to Fate");

        var playerIndex = 0;
        _logger.Print($"Choosing player {GetUsername(fateablePlayers[playerIndex])}");

        return await Task.FromResult(playerIndex);
    }

    public async Task<int> ChooseFateCard(List<CardState> cards)
    {
        _logger.Print($"Choose which Fate card you want to use: {cards.Select(x => x.Name).ToNiceString()}");

        var cardIndex = 0;
        _logger.Print($"Choosing card {cards[cardIndex].Name}");
        return await Task.FromResult(cardIndex);
    }

    public async Task<int> ChooseFateLocation(List<LocationState> locations)
    {
        _logger.Print($"Choose where you want to put the Fate card: {locations.Select(x => x.Name).ToNiceString()}");

        var locationIndex = 0;
        _logger.Print($"Choosing location {locations[locationIndex].Name}");
        return await Task.FromResult(locationIndex);
    }

    public async Task<int> ChooseHeroToAddFateItemTo(List<CardState> heroes)
    {
        _logger.Print($"Choose the hero to whom you want to add the item: {heroes.Select(x => x.Name).ToNiceString()}");

        var heroIndex = 0;
        _logger.Print($"Choosing hero {heroes[heroIndex].Name}");
        return await Task.FromResult(heroIndex);
    }

    public async Task<List<int>> ChooseCardsToDiscard(List<CardState> cards, int? min, int? max)
    {
        _logger.Print($"Choose which cards you want to discard (pick between {min ?? 0} and {max ?? cards.Count}): {cards.Select(x => x.Name).ToNiceString()}");
        
        var cardIndexes = ChooseCards(cards, min, max);
        _logger.Print($"Discarding cards [{cardIndexes.Select(x => cards[x].Name).ToNiceString()}]");
        return await Task.FromResult(cardIndexes);
    }

    private static List<int> ChooseCards(IEnumerable<CardState> cards, int? min, int? max)
    {
        int GetScore(CardState card) => card.Type switch { CardType.VillainSpecial => 1, CardType.Condition => -1, _ => 0 };
        var cardsByScore = cards.Select((card, index) => (card, index, score: GetScore(card))).OrderBy(x => x.score).ToList();
        var badCardCount = cardsByScore.Count(x => x.score < 0);
        var maxCardsToDiscard = Math.Min(badCardCount, max ?? badCardCount);
        var amountOfCardsToDiscard = Math.Max(min ?? 0, maxCardsToDiscard);
        return Enumerable.Range(0, amountOfCardsToDiscard).Select(i => cardsByScore[i].index).ToList();
    }

    public async Task<int> ChooseHeroToVanquish(List<CardState> defeatableHeroes)
    {
        _logger.Print($"Choose which Hero you want to vanquish: {defeatableHeroes.Select(x => $"{x.Name} ({x.Strength} Strength)").ToNiceString()}");

        var heroIndex = 0;
        _game.VanquishingHero = defeatableHeroes[heroIndex];
        _logger.Print($"Choosing Hero {_game.VanquishingHero.Name} ({_game.VanquishingHero.Strength} Strength)");
        return await Task.FromResult(heroIndex);
    }

    public async Task<List<int>> ChooseAlliesForVanquish(List<CardState> availableAllies, int requiredAllyCount)
    {
        _logger.Print($"Choose the Allies you want to use to defeat the Hero: {availableAllies.Select(x => $"{x.Name} ({x.Strength} Strength)").ToNiceString()}");

        var chosenAllyIndexes = FindClosestSum(availableAllies.Select((x, i) => (x.Strength ?? 0, i)).ToList(), _game.VanquishingHero!.Strength ?? 0, requiredAllyCount);
        var chosenAllies = chosenAllyIndexes.Select(x => availableAllies[x]).ToList();
        _logger.Print($"Choosing Allies {chosenAllies.Select(x => $"{x.Name} ({x.Strength} Strength)")}");
        return await Task.FromResult(chosenAllyIndexes);
    }

    public async Task<bool> ChoosePerformSpecial(CardState card)
    {
        _logger.Print($"Do you want to execute the special of card {card.Name}?");
        _logger.Print("Choosing yes");
        return await Task.FromResult(true);
    }

    public async Task<int> ChooseSpecialLocation(CardState card, List<LocationState> availableLocations)
    {
        _logger.Print($"Which location do you want for card {card.Name}? {availableLocations.Select(x => x.Name).ToNiceString()}");

        _logger.Print("Choosing first location");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseSpecialAction(CardState card, List<ActionState> availableActions)
    {
        _logger.Print($"Which action do you want to execute for card {card.Name}? {availableActions.Select(x => x.Type).ToNiceString()}");

        _logger.Print("Choosing first action");
        return await Task.FromResult(0);
    }

    public async Task<int?> ChooseSpecialCard(CardState card, List<CardState> availableCards)
    {
        _logger.Print($"Which action do you want to execute for card {card.Name}? {availableCards.Select(x => x.Name).ToNiceString()}");

        _logger.Print("Choosing first card");
        return await Task.FromResult(0);
    }

    public async Task<int> ChooseCardToActivate(List<CardState> availableCards)
    {
        _logger.Print($"Which card do you want to activate? {availableCards.Select(x => x.Name).ToNiceString()}");

        _logger.Print("Choosing first card");
        return await Task.FromResult(0);
    }

    public async Task<bool> ActivateCondition(CardState card)
    {
        _logger.Print($"Do you want to activate the condition of card {card.Name}?");
        _logger.Print("Choosing yes");
        return await Task.FromResult(true);
    }

    public async Task PlayerWonGame(Guid winningPlayerId)
    {
        _logger.Print($"{GetUsername(winningPlayerId)} has won the game!");
        await Connection.LeaveGame();
    }
    
    private static List<T> FindClosestSum<T>(IReadOnlyList<(int num, T value)> nums, int target, int minOptions)
    {
        var currentSubset = new List<(int num, T value)>();
        var bestSubset = new List<(int num, T value)>();
        var bestSum = int.MaxValue;

        void Backtrack(int index)
        {
            var currentSum = currentSubset.Sum(x => x.num);

            if (currentSubset.Count >= minOptions && currentSum >= target && currentSum < bestSum)
            {
                bestSum = currentSum;
                bestSubset = currentSubset.ToList();
                return;
            }

            for (var i = index; i < nums.Count; i++)
            {
                currentSubset.Add(nums[i]);
                Backtrack(i + 1);
                currentSubset.RemoveAt(currentSubset.Count - 1);
            }
        }

        Backtrack(0);

        return bestSubset.Select(x => x.value).ToList();
    }
}