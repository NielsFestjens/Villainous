using Microsoft.AspNetCore.Mvc;

namespace Villainous.Server.Application;

public static class UserActions
{
    public static LoginResult Register(AppConfig appConfig, UserContext userContext, [FromBody] Credentials data) => HandleLogin(appConfig, userContext.Register(data.Username, data.Password));
    public static LoginResult Login(AppConfig appConfig, UserContext userContext, [FromBody] Credentials data) => HandleLogin(appConfig, userContext.Login(data.Username, data.Password));

    private static LoginResult HandleLogin(AppConfig appConfig, Guid id) => new(id, JWTHelper.GenerateJSONWebToken(appConfig, id));

    public record Credentials(string Username, string Password);
    public record LoginResult(Guid Id, string AccessToken);
}

public static class ApplicationActionMapper
{
    public static void Map(WebApplication app)
    {
        DynamicEndpointMapper.MapCommands(app, typeof(ApplicationActionMapper).Assembly);
        DynamicEndpointMapper.MapRequests(app, typeof(ApplicationActionMapper).Assembly);

        MapAnonymousPost(app, "User", nameof(UserActions.Register), UserActions.Register);
        MapAnonymousPost(app, "User", nameof(UserActions.Login), UserActions.Login);
    }

    private static RouteHandlerBuilder MapPost(IEndpointRouteBuilder app, string domain, string name, Delegate handler)
    {
        return app.MapPost($"/{domain}/{name}", handler).WithName($"{domain}.{name}").WithOpenApi().RequireAuthorization();
    }

    private static RouteHandlerBuilder MapAnonymousPost(IEndpointRouteBuilder app, string domain, string name, Delegate handler)
    {
        return app.MapPost($"/{domain}/{name}", handler).WithName($"{domain}.{name}").WithOpenApi();
    }

    private static RouteHandlerBuilder MapGet(IEndpointRouteBuilder app, string domain, string name, Delegate handler)
    {
        return app.MapGet($"/{domain}/{name}", handler).WithName($"{domain}.{name}").WithOpenApi().RequireAuthorization();
    }
}