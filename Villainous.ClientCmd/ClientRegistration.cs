namespace Villainous.ClientCmd;

public class ClientRegistration
{

    public static async Task<LoginInfo> Register(ILogger logger, VillainousClientConfig config)
    {
        var username = config.Username;
        if (username == null)
        {
            await Console.Out.WriteLineAsync("What's your username?");
            username = await Console.In.ReadLineAsync();
        }

        logger.Print($"Welcome {username}!");

        var httpClient = new HttpClient { BaseAddress = new Uri(config. BaseAddress) };

        var loginInfo = await httpClient.PostJson("User/Register", new { Username = username, Password = "SecurePasswordsAreImportant!!!111OneEleven" }).GetAsJson<LoginInfo>();
        httpClient.AuthenticateWithBearer(loginInfo.AccessToken);
        logger.LogDebug("Registered!");
        return loginInfo;
    }
}

public record LoginInfo(Guid Id, string AccessToken);