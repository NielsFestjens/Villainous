import React from 'react';
import './App.css';
import { GameContext, VillainousClientConfig } from './GameContext';
import { VillainousClient } from './VillainousClient';

var config: VillainousClientConfig = {
    baseAddress: "https://localhost:7161",
    isFirstUser: false,
};
const client = new VillainousClient();
client.start(config);

const App = () => {
  return (
    <GameContext.Provider value={client}>
        <div className="App">
        <header className="App-header">
            Hello
        </header>
        </div>
    </GameContext.Provider>
  );
}

export default App;
