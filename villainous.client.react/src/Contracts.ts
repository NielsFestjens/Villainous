
export type Guid = string;

export type User = { id: Guid; username: string; isAvailable: boolean; }
export type PlayerInfoDto = { index: number, userId: Guid, villainInfo: VillainInfoDto };
export type VillainInfoDto = { edition: string, name: string, locations: LocationInfoDto[], cards: CardInfoDto[] };
export type CardInfoDto = { name: string, type: CardType, wikiName: string, description: string, cost?: number, strength?: number, amount: number };
export type LocationInfoDto = { name: string, heroLocationActions: ActionInfoDto[], allyLocationActions: ActionInfoDto[], startsLocked: boolean };
export type ActionInfoDto = { type: ActionType, amount?: number };

export type GameState = { player: PlayerState, otherPlayers: PlayerPublicState[] };
export type PlayerState = { power: number, villainLocationIndex: number, hand: CardState, locations: LocationState[] };
export type CardState = { name: string, cost: (number | undefined)[], strength? : number, cards: CardState[], cardLocation?: CardLocation, type: CardType };
export type LocationState = { index: number, name: string, actions: ActionState[], allyCards: CardState[], heroCards: CardState[] };
export type ActionState = { index: number, type: ActionType, isAvailable: boolean };
export type PlayerPublicState = { id: Guid, power: number, villainLocationIndex: number, hand: CardState[], locations: LocationState[] };

export type CardLocation = { locationIndex: number, type: LocationType, cardIndex?: number };

export enum LocationType
{
    Hero = 1,
    Ally = 2
}

export enum CardType
{
    Ally = 1,
    AllyItem,
    VillainItem,
    VillainEffect,
    Condition,
    VillainSpecial,

    Hero = 11,
    HeroItem,
    FateEffect,
}

export enum ActionType
{
    GainPower = 1,
    MoveItemOrAlly,
    MoveHero,
    PlayCard,
    Fate,
    DiscardCards,
    Vanquish,
    Activate
}

export class VillainNames
{
    static readonly Maleficent = "Maleficent";
}