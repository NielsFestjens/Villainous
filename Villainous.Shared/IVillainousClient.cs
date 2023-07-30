namespace Villainous;

public interface ILobbyClient
{
    Task PlayerJoinedLobby(Guid userId, string name, bool isAvailable);
    Task GameCreated(Guid gameId);
    Task PlayerIsPlayingGame(Guid userId);
    Task PlayerStoppedPlayingGame(Guid userId);
}

public interface IVillainousClient
{
    Task PlayerJoinedGame(Guid id);
    Task PlayerLeftGame(Guid id);
    Task GameReadyToStart(int amountOfPlayers);
    Task GameStarted(List<PlayerInfoDto> playerInfos);
    Task TurnStarted(Guid userId, GameState playerState);
    Task StartTurnRequested();
    Task<int> MoveVillain(List<LocationState> locations, int roundNumber);
    Task<int?> ChooseAction(List<ActionState> actions, PlayerState playerState);
    Task<int> ChooseItemOrAllyToMove(List<CardState> cards);
    Task<int> ChooseLocationToMoveItemOrAllyTo(List<LocationState> locations);
    Task<int> ChooseHeroToMove(List<CardState> cards);
    Task<int> ChooseLocationToMoveHeroTo(List<LocationState> locations);
    Task<int> ChooseCardToPlay(List<CardState> cards);
    Task<int> ChooseLocationToPlayCardTo(List<LocationState> locations);
    Task<int> ChooseAllyToAddItemTo(List<CardState> allies);
    Task<int> ChooseFateTargetPlayer(List<Guid> fateablePlayers);
    Task<int> ChooseFateCard(List<CardState> cards);
    Task<int> ChooseFateLocation(List<LocationState> locations);
    Task<int> ChooseHeroToAddFateItemTo(List<CardState> heroes);
    Task<List<int>> ChooseCardsToDiscard(List<CardState> cards, int? min, int? max);
    Task<int> ChooseHeroToVanquish(List<CardState> defeatableHeroes);
    Task<List<int>> ChooseAlliesForVanquish(List<CardState> availableAllies, int requiredAllyCount);
    Task<bool> ChoosePerformSpecial(CardState card);
    Task<int> ChooseSpecialLocation(CardState card, List<LocationState> availableLocations);
    Task<int> ChooseSpecialAction(CardState card, List<ActionState> availableActions);
    Task<int?> ChooseSpecialCard(CardState card, List<CardState> availableCards);
    Task<int> ChooseCardToActivate(List<CardState> availableCards);
    Task<bool> ActivateCondition(CardState card);
    Task PlayerWonGame(Guid winningPlayerId);
}

public interface IGameClient : ILobbyClient, IVillainousClient
{
}

public interface IGameServer
{
    Task JoinLobby();
    Task CreateGame();
    Task TriggerLobby();
    Task JoinGame(Guid id);
    Task ChooseVillain(string villainName);
    Task StartGame();
    Task StartTurn();
    Task LeaveGame();
}