import React from 'react';
import { VillainousClient } from './VillainousClient';

export type VillainousClientConfig = {
    baseAddress: string;
    isFirstUser: boolean;
}

export const GameContext = React.createContext<VillainousClient>(undefined as any);