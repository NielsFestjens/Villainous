import { GameConnection } from "./GameConnection";
import { LobbyClient } from './LobbyClient';
import { VillainousClientConfig } from './GameContext';
import { Guid, PlayerInfoDto, GameState, LocationState, ActionState, PlayerState, CardState, VillainInfoDto, VillainNames, CardType } from './Contracts';
import { ClientRegistration, LoginInfo } from './ClientRegistration';

export class Game
{
    villainName: string = null!;
    players: PlayerInfoDto[] = null!;
    villainInfo: VillainInfoDto = null!;

    villainLocationIndex: number = 0;
    power: number = 0;
    hand: CardState[] = null!;
    vanquishingHero?: CardState;
    playerState: PlayerState = null!;

    isVillain = (villainName: string) => this.villainName === villainName;
}

export class VillainousClient {
    lobby: LobbyClient;

    config: VillainousClientConfig = null!;
    loginInfo: LoginInfo = null!;
    connection: GameConnection = null!;
    game: Game = new Game();

    public get userId() { return this.loginInfo.id; }
    public get isFirstUser() { return this.config.isFirstUser; }

    getUsername = (id: Guid) => this.lobby.lobbyUsers[id].username;

    constructor() {
        this.lobby = new LobbyClient(this);
    }

    start = async (config: VillainousClientConfig) => {
        this.config = config;
        this.loginInfo = await ClientRegistration.Register(config);
        this.connection = await GameConnection.ConfigureConnection(this, config, this.loginInfo);
        await this.connection.joinLobby();
    };

    playerJoinedGame = async (id: Guid) => {
        if (id !== this.loginInfo.id)
        {
            console.log(`${this.getUsername(id)} joined the game`);
            return;
        }

        console.log("You joined the game. Choose a villain");
        this.game.villainName = "Maleficent";

        console.log(`Choosing ${this.game.villainName}`);
        await this.connection.chooseVillain(this.game.villainName);
    };

    playerLeftGame = async (id: Guid) => {
        console.log(`${this.getUsername(id)} left the game. Sadsies :'(`);
    };

    gameReadyToStart = async (amountOfPlayers: number) => {
        console.log(`The game is ready to start, there are ${amountOfPlayers} players`);

        if (this.config.isFirstUser)
        {
            console.log("Starting the game");
            await this.connection.startGame();
        }
    };

    gameStarted = async (playerInfos: PlayerInfoDto[]) => {
        this.game.players = playerInfos;
        this.game.villainInfo = playerInfos.find(x => x.userId === this.loginInfo.id)!.villainInfo;

        console.log("The game has started, these are the players with their villains", playerInfos);
    };

    turnStarted = async (userId: Guid, gameState: GameState) => {
        console.log(`Turn started for ${this.getUsername(userId)}.`);
        this.updateState(gameState.player);

        if (userId !== this.loginInfo.id)
            console.log(`Waiting for ${this.getUsername(userId)} to make their move.`);
    };

    updateState = async (playerState: PlayerState) => {
        console.log(playerState);
        this.game.playerState = playerState;
        this.game.villainLocationIndex = playerState.villainLocationIndex;
    }

    startTurnRequested = async () => {
        console.log("Start turn requested");

        console.log("Starting turn");
        this.connection.startTurn();
    };

    moveVillain = async (locations: LocationState[], roundNumber: number) => {
        console.log(`It's your turn (round ${roundNumber}). Move your villain. You are at position ${this.game.villainLocationIndex}.`);

        var index = locations.map((location, i) => ({location, i})).find(x => x.location.index > this.game.villainLocationIndex)?.i ?? 0;
        this.game.villainLocationIndex = locations[index].index;
        console.log(`Moving to ${this.game.villainInfo.locations[this.game.villainLocationIndex].name}(position index ${this.game.villainLocationIndex})`);
        return index;
    };

    chooseAction = async (actions: ActionState[], playerState: PlayerState) => {
        this.updateState(playerState);
        console.log(`Choose an action. Actions: [${actions.map(x => `[${x.index}] ${x.type}${(x.isAvailable ? "" : " (not available)")}`)}]`);

        var chosenAction = actions.find(x => x.isAvailable);
        console.log(chosenAction == null ? "Stopping with actions" : `Choosing action {chosenAction.Index}: {chosenAction.Type}`);

        return chosenAction?.index;
    };

    chooseItemOrAllyToMove = async (cards: CardState[]) => {
        console.log(`Choose the ally or item that you want to move: ${cards.map(x => x.name)}`);
        return 0;
    };

    chooseLocationToMoveItemOrAllyTo = async (locations: LocationState[]) => {
        console.log("Choose a location to where you want to move the ally or item");
        return 0;
    };

    chooseHeroToMove = async (cards: CardState[]) => {
        console.log(`Choose the hero that you want to move: ${cards.map(x => x.name)}`);
        return 0;
    };

    chooseLocationToMoveHeroTo = async (locations: LocationState[]) => {
        console.log("Choose a location to where you want to move the hero");
        return 0;
    };

    chooseCardToPlay = async (cards: CardState[]) => {
        console.log(`Choose which card you want to play. Cards: ${cards.map(x => x.name)}`);
        var chosenCardIndex = 0;
        console.log(`Choosing card ${cards[chosenCardIndex].name} (index ${chosenCardIndex})`);

        return chosenCardIndex;
    };

    chooseLocationToPlayCardTo = async (locations: LocationState[]) => {
        console.log(`Choose where you want to put the card: ${locations.map(x => x.name)}`);

        var locationIndex = this.getLocationToPlayCardTo(locations);
        console.log(`Choosing location ${locations[locationIndex].name}`);
        return locationIndex;
    };

    getLocationToPlayCardTo = (locations: LocationState[]) => {
        if (this.game.isVillain(VillainNames.Maleficent))
        {
            for (var i = 0; i < locations.length; i++)
            {
                if (!locations[i].allyCards.some(x => x.type === CardType.VillainSpecial))
                    return i;
            }
        }

        return 0;
    }

    chooseAllyToAddItemTo = async (allies: CardState[]) => {
        console.log(`Choose the ally to whom you want to add the item: ${allies.map(x => x.name)}`);

        var allyIndex = 0;
        console.log(`Choosing ally ${allies[allyIndex]}`);
        return allyIndex;
    };

    chooseFateTargetPlayer = async (fateablePlayers: Guid[]) => {
        console.log("Choose a player to Fate");

        var playerIndex = 0;
        console.log(`Choosing player ${this.getUsername(fateablePlayers[playerIndex])}`);

        return playerIndex;
    };

    chooseFateCard = async (cards: CardState[]) => {
        console.log(`Choose which Fate card you want to use: ${cards.map(x => x.name)}`);

        var cardIndex = 0;
        console.log(`Choosing card ${cards[cardIndex].name}`);
        return cardIndex;
    };

    chooseFateLocation = async (locations: LocationState[]) => {
        console.log(`Choose where you want to put the Fate card: ${locations.map(x => x.name)}`);

        var locationIndex = 0;
        console.log(`Choosing location ${locations[locationIndex].name}`);
        return locationIndex;
    };

    chooseHeroToAddFateItemTo = async (heroes: CardState[]) => {
        console.log(`Choose the hero to whom you want to add the item: ${heroes.map(x => x.name)}`);

        var heroIndex = 0;
        console.log(`Choosing hero ${heroes[heroIndex].name}`);
        return heroIndex;
    };

    chooseCardsToDiscard = async (cards: CardState[], min?: number, max?: number) => {
        console.log(`Choose which cards you want to discard (pick between ${min ?? 0} and ${max ?? cards.length}): ${cards.map(x => x.name)}`);
        
        var cardIndexes = this.chooseCards(cards, min, max);
        console.log(`Discarding cards [${cardIndexes.map(x => cards[x].name)}]`);
        return cardIndexes;
    };

    chooseCards = (cards: CardState[], min?: number, max?: number) => {
        const getScore = (card: CardState) => card.type === CardType.VillainSpecial ? 1 : card.type === CardType.Condition ? -1 : 0;
        var cardsByScore = cards.map((card, index) => ({card, index, score: getScore(card)}));
        cardsByScore.sort((a, b) => a.score - b.score)
        var badCardCount = cardsByScore.filter(x => x.score < 0).length;
        var maxCardsToDiscard = Math.min(badCardCount, max ?? badCardCount);
        var amountOfCardsToDiscard = Math.max(min ?? 0, maxCardsToDiscard);
        return Array.from(new Array(amountOfCardsToDiscard), (x, i) => i).map(i => cardsByScore[i].index);
    }

    chooseHeroToVanquish = async (defeatableHeroes: CardState[]) => {
        console.log(`Choose which Hero you want to vanquish: ${defeatableHeroes.map(x => `${x.name} (${x.strength} Strength)`)}`);

        var heroIndex = 0;
        this.game.vanquishingHero = defeatableHeroes[heroIndex];
        console.log(`Choosing Hero ${this.game.vanquishingHero.name} (${this.game.vanquishingHero.strength} Strength)`);
        return heroIndex;
    };

    chooseAlliesForVanquish = async (availableAllies: CardState[], requiredAllyCount: number) => {
        console.log(`Choose the Allies you want to use to defeat the Hero: ${availableAllies.map(x => `${x.name} (${x.strength} Strength)`)}`);

        var chosenAllyIndexes = this.findClosestSum(availableAllies.map((x, i) => ({num: x.strength ?? 0, value: i})), this.game.vanquishingHero!.strength ?? 0, requiredAllyCount);
        var chosenAllies = chosenAllyIndexes.map(x => availableAllies[x]);
        console.log(`Choosing Allies ${chosenAllies.map(x => `${x.name} (${x.strength} Strength)`)}`);
        return chosenAllyIndexes;
    };

    choosePerformSpecial = async (card: CardState) => {
        console.log(`Do you want to execute the special of card ${card.name}?`);
        console.log("Choosing yes");
        return true;
    };

    chooseSpecialLocation = async (card: CardState, availableLocations: LocationState[]) => {
        console.log(`Which location do you want for card ${card.name}? ${availableLocations.map(x => x.name)}`);

        console.log("Choosing first location");
        return 0;
    };

    chooseSpecialAction = async (card: CardState, availableActions: ActionState[]) => {
        console.log(`Which action do you want to execute for card ${card.name}? ${availableActions.map(x => x.type)}`);

        console.log("Choosing first action");
        return 0;
    };

    chooseSpecialCard = async (card: CardState, availableCards: CardState[]) => {
        console.log(`Which action do you want to execute for card ${card.name}? ${availableCards.map(x => x.name)}`);

        console.log("Choosing first card");
        return 0;
    };

    chooseCardToActivate = async (availableCards: CardState[]) => {
        console.log(`Which card do you want to activate? ${availableCards.map(x => x.name)}`);

        console.log("Choosing first card");
        return 0;
    };

    activateCondition = async (card: CardState) => {
        console.log(`Do you want to activate the condition of card ${card.name}?`);
        console.log("Choosing yes");
        return true;
    };

    playerWonGame = async (winningPlayerId: Guid) => {
        console.log(`${this.getUsername(winningPlayerId)} has won the game!`);
        await this.connection.leaveGame();
    };
    
    findClosestSum = <T>(nums: {num: number, value: T}[], target: number, minOptions: number) =>
    {
        var currentSubset: {num: number, value: T}[] = [];
        var bestSubset: {num: number, value: T}[] = [];
        var bestSum = Number.MAX_VALUE;

        const Backtrack = (index: number) => {
            var currentSum = currentSubset.reduce((sum, x) => sum + x.num, 0);

            if (currentSubset.length >= minOptions && currentSum >= target && currentSum < bestSum)
            {
                bestSum = currentSum;
                bestSubset = [...currentSubset];
                return;
            }

            for (var i = index; i < nums.length; i++)
            {
                currentSubset.push(nums[i]);
                Backtrack(i + 1);
                currentSubset.splice(currentSubset.length - 1, 1);
            }
        }

        Backtrack(0);

        return bestSubset.map(x => x.value);
    }
}
