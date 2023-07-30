using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Villainous.Server.Application;

public static class ApplicationServicesConfigurator
{
    public static void Configure(IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddCors(o => o.AddDefaultPolicy(builder =>
        {
            builder.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }));

        services.AddLogging(logging => logging.AddSimpleConsole(x => x.SingleLine = true));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddSignalR(hubOptions =>
        {
            hubOptions.EnableDetailedErrors = true;
            hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
            hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(5);
        });
        services.AddTransient<GameHub>();

        var appConfig = configuration.Get<AppConfig>()!;

        services.AddSingleton(appConfig);
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    ValidIssuer = appConfig.Jwt.Issuer,
                    ValidAudience = appConfig.Jwt.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfig.Jwt.Key)),
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        await Task.CompletedTask;
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                            context.Token = accessToken;
                    }
                };
            });

        services.AddSingleton<UserContext>();
        services.AddSingleton<Lobby>();
        services.AddSingleton<VillainInfoLoader>();
        services.AddSingleton<VillainLoader>();
        services.AddTransient<ILogger>(x => x.GetRequiredService<ILogger<_>>());

        services.AddHttpContextAccessor();
    }
}