using Villainous.Server.Application;

var builder = WebApplication.CreateBuilder(args);
ApplicationServicesConfigurator.Configure(builder.Services, builder.Configuration);

var app = builder.Build();
ApplicationConfigurator.Configure(app);
ApplicationActionMapper.Map(app);

app.Run();

public class _ { }