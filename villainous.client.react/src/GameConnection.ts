import * as signalR from "@microsoft/signalr";
import { VillainousClientConfig } from "./GameContext";
import { Guid } from "./Contracts";
import { VillainousClient } from './VillainousClient';
import { LoginInfo } from "./ClientRegistration";


export class GameConnection {
    connection: signalR.HubConnection;

    constructor(connection: signalR.HubConnection) {
        this.connection = connection;
    }

    static ConfigureConnection = async (client: VillainousClient, config: VillainousClientConfig, loginInfo: LoginInfo) => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${config.baseAddress}/gameHub`, {
                accessTokenFactory: () => loginInfo.accessToken
            })
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.autoWire(client, connection);
        this.autoWire(client.lobby, connection);

        await connection.start();

        return new GameConnection(connection);
    };

    static autoWire = (client: any, connection: signalR.HubConnection) => {
        const allMethods = Object.getOwnPropertyNames(client);
        for (const methodName of allMethods) {
            const method = (client as any)[methodName];
            if (typeof method === 'function')
                connection.on(methodName, method);
        }
    };

    joinLobby = async () => await this.connection.invoke("JoinLobby");
    createGame = async () => await this.connection.invoke("CreateGame");
    triggerLobby = async () => await this.connection.invoke("TriggerLobby");
    joinGame = async (id: Guid) => await this.connection.invoke("JoinGame", id);
    chooseVillain = async (villainName: string) => await this.connection.invoke("ChooseVillain", villainName);
    startGame = async () => await this.connection.invoke("StartGame");
    startTurn = async () => await this.connection.invoke("StartTurn");
    leaveGame = async () => await this.connection.invoke("LeaveGame");
}
