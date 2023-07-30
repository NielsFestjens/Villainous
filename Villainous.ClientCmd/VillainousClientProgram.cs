using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSimpleConsole(x => { x.SingleLine = true; });
builder.Services.AddTransient<ILogger>(x => x.GetRequiredService<ILogger<_>>());
builder.Services.AddSingleton<VillainousClient>();
builder.Services.AddHostedService<VillainousClientProgram>();
builder.Services.AddSingleton(new ArgsProvider(args));

var host = builder.Build();
await host.StartAsync();
await host.WaitForShutdownAsync();

public class _ { }

public record ArgsProvider(string[] Args);

public class VillainousClientProgram : BackgroundService
{
    private readonly string[] _args;
    private readonly VillainousClient _client;

    public VillainousClientProgram(ArgsProvider argsProvider, VillainousClient client)
    {
        _client = client;
        _args = argsProvider.Args;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var arguments = _args.Select(x => x.Split('=')).ToDictionary(x => x[0], x => x.Length > 1 ? x[1] : "true");
            var process = arguments.TryGetValue("spawn", out var spawnName)
                ? Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule!.FileName, $"name={spawnName}") { UseShellExecute = true })
                : default;

            arguments.TryGetValue("name", out var name);
            arguments.TryGetValue("startGame", out var startGame);

            await _client.Start(new VillainousClientConfig
            {
                BaseAddress = "https://localhost:7161",
                Username = name,
                IsFirstUser = startGame == "true"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.ReadKey();
        }
    }
}