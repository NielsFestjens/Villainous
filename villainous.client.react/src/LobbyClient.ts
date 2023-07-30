import { Guid, User } from './Contracts';
import { VillainousClient } from './VillainousClient';


export class LobbyClient {
    villainousClient: VillainousClient;
    lobbyUsers: { [id: Guid] : User; } = {};
    getName = (id: Guid) => id === this.villainousClient.userId ? "You" : this.lobbyUsers[id].username;
    gameIds: Guid[] = [];

    constructor(villainousClient: VillainousClient) {
        this.villainousClient = villainousClient;
    }

    joinLobby = async () => {
        await this.villainousClient.connection.joinLobby();
    };

    playerJoinedLobby = async (userId: Guid, name: string, isAvailable: boolean) => {
        if (this.lobbyUsers[userId])
        {
            this.lobbyUsers[userId].isAvailable = isAvailable;
            return;
        }

        this.lobbyUsers[userId] = { id: userId, username: name, isAvailable: isAvailable };
        console.log(`${this.getName(userId)} joined the lobby`);

        if (userId === this.villainousClient.userId && this.villainousClient.isFirstUser)
        {
            console.log("Creating a game");
            await this.villainousClient.connection.createGame();
        }
    };

    gameCreated = async (gameId: Guid) => {
        var isNewGame = !this.gameIds.includes(gameId);
        if (isNewGame)
            this.gameIds.push(gameId);

        if (!isNewGame || this.gameIds.length > 1)
            return;

        if (this.villainousClient.isFirstUser)
        {
            console.log("Your game is created");
        }
        else
        {
            console.log("A game was created, do you want to join it?");

            console.log("Joining game");
            await this.villainousClient.connection.joinGame(gameId);
        }
    };

    playerIsPlayingGame = async (userId: Guid) => {
        this.lobbyUsers[userId].isAvailable = false;
    };

    playerStoppedPlayingGame = async (userId: Guid) => {
        this.lobbyUsers[userId].isAvailable = true;
    };

}
