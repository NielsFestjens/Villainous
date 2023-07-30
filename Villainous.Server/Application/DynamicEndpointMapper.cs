using System.Reflection;

namespace Villainous.Server.Application;

public class DynamicEndpointMapper
{
    public static void MapCommands(WebApplication app, Assembly assembly)
    {
        var commandHandlerType = typeof(ICommandHandler<>);
        var mapPost = typeof(DynamicEndpointMapper).GetMethod(nameof(MapPost), BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var type in assembly.GetTypes())
        {
            var handlerInterfaces = type.GetInterfaces().Where(x => x.IsAssignableTo(commandHandlerType)).ToList();
            foreach (var handlerInterface in handlerInterfaces)
            {
                var commandType = handlerInterface.GetGenericArguments().Single();
                var genericMapPost = mapPost.MakeGenericMethod(handlerInterface, commandType);
                var name = type.FullName!.Split('.').Reverse().Skip(1).First();
                genericMapPost.Invoke(null, new object[] { app, name });
            }
        }
    }

    public static void MapRequests(WebApplication app, Assembly assembly)
    {
        var requestHandlerType = typeof(IRequestHandler<,>);
        var mapGet = typeof(DynamicEndpointMapper).GetMethod(nameof(MapGet), BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var type in assembly.GetTypes())
        {
            var interfaces = type.GetInterfaces().Where(x => x.IsAssignableTo(requestHandlerType)).ToList();
            foreach (var commandHandlerInterface in interfaces)
            {
                var arguments = commandHandlerInterface.GetGenericArguments();
                var genericMapGet = mapGet.MakeGenericMethod(commandHandlerInterface, arguments[0], arguments[1]);
                var name = type.FullName!.Split('.').Reverse().Skip(1).First();
                genericMapGet.Invoke(null, new object[] { app, name });
            }
        }
    }

    private static void MapPost<THandler, TCommand>(IEndpointRouteBuilder app, string name) where THandler : ICommandHandler<TCommand>
    {
        app.MapPost($"/{name}", (THandler handler, TCommand command) => handler.Handle(command)).WithName(name).WithOpenApi();
    }

    private static void MapGet<THandler, TRequest, TResponse>(IEndpointRouteBuilder app, string name) where THandler : IRequestHandler<TRequest, TResponse>
    {
        app.MapGet($"/{name}", (THandler handler, TRequest request) => handler.Handle(request)).WithName(name).WithOpenApi();
    }

    public interface ICommandHandler<in TCommand>
    {
        void Handle(TCommand command);
    }

    public interface ICommandWithResultHandler<in TCommand, out TResult>
    {
        TResult Handle(TCommand command);
    }

    public interface IRequestHandler<in TRequest, out TResponse>
    {
        TResponse Handle(TRequest request);
    }
}