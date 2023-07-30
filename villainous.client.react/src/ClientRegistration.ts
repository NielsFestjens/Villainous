import { Guid } from './Contracts';
import { VillainousClientConfig } from './GameContext';


export class ClientRegistration {
    static Register = async (config: VillainousClientConfig) => {
        const response = await fetch(`${config.baseAddress}/User/Register`, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                Username: "Willy",
                Password: "SecurePasswordsAreImportant!!!111OneEleven"
            })
        });

        return await response.json() as LoginInfo;
    };
}

export type LoginInfo = {
    id: Guid;
    accessToken: string;
}
