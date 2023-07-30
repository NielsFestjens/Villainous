using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Villainous.ClientCmd;

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSimpleConsole(x => { x.SingleLine = true; });
builder.Services.AddTransient<ILogger>(x => x.GetRequiredService<ILogger<_>>());
builder.Services.AddSingleton<VillainousClient>();
builder.Services.AddHostedService<VillainousClientProgram>();
builder.Services.AddSingleton(new ArgsProvider(args));

var host = builder.Build();
await host.StartAsync();
await host.WaitForShutdownAsync();