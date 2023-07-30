namespace Villainous.Server.Game;

public interface IEffectEvent { }
public interface IEffectWithResultEvent { }
public interface IEffectWithResultEvent<TReturn> : IEffectWithResultEvent { }

public record CalculateAreCardsRevealedEvent : IEffectWithResultEvent<bool>;
public record CalculateNeedsToMoveEvent : IEffectWithResultEvent<bool>;
public record VillainMovedEvent(Location Location) : IEffectEvent;
public record CalculateCardStrengthBonusEvent(Card Card) : IEffectWithResultEvent<int?>;
public record CalculateCardCostBonusEvent(Card Card, Location? Location) : IEffectWithResultEvent<int?>;
public record CalculateCanPlayCardEvent(Card Card, Location Location) : IEffectWithResultEvent<bool>;
public record CardPlayedEvent(Card Card) : IEffectEvent;
public record CalculateRequiredAllyCountEvent(Card Hero) : IEffectWithResultEvent<int>;
public record HeroVanquishedEvent(Card Card, int HeroStrength) : IEffectEvent;
public record CalculateCanFateEvent(Card Card, CardLocation Location) : IEffectWithResultEvent<bool>;
public record FatedEvent(Card Card) : IEffectEvent;

public enum Duration
{
    UntilDiscarded,
    UntilStartOfNextTurn,
    UntilAfterVillainMovePhase,
}

public class EventHandler
{
    private readonly Dictionary<Type, List<(Card card, Player player, bool self, Action<Player, Card, IEffectEvent> handler, Duration duration)>> _eventHandlers = new();
    private readonly Dictionary<Type, List<(Card card, Player player, bool self, Func<IGameHub, Player, Card, IEffectEvent, Task> handler, Duration duration)>> _asyncEventHandlers = new();
    private readonly Dictionary<Type, List<(Card card, Player player, bool self, Func<Player, Card, IEffectWithResultEvent, object?> handler, Duration duration)>> _eventWithResultHandlers = new();

    public void RegisterOnSelf<TEvent>(Player player, Card card, Action<Player, Card, TEvent> eventHandler, Duration duration = Duration.UntilDiscarded) where TEvent : IEffectEvent
    {
        if (!_eventHandlers.ContainsKey(typeof(TEvent)))
            _eventHandlers[typeof(TEvent)] = new();

        void Handler(Player triggeringPlayer, Card localCard, IEffectEvent effectEvent)
        {
            eventHandler(player, localCard, (TEvent)effectEvent);
        }

        _eventHandlers[typeof(TEvent)].Add((card, player, true, Handler, duration));
    }

    public void RegisterOnOthersAsync<TEvent>(Player player, Card card, Func<IGameHub, Player, Card, TEvent, Task> eventHandler, Duration duration = Duration.UntilDiscarded) where TEvent : IEffectEvent
    {
        if (!_asyncEventHandlers.ContainsKey(typeof(TEvent)))
            _asyncEventHandlers[typeof(TEvent)] = new();

        async Task Handler(IGameHub gameHub, Player triggeringPlayer, Card localCard, IEffectEvent effectEvent)
        {
            await eventHandler(gameHub, player, localCard, (TEvent)effectEvent);
        }

        _asyncEventHandlers[typeof(TEvent)].Add((card, player, false, Handler, duration));
    }

    public void RegisterOnSelf<TEvent, TReturn>(Player player, Card card, Func<Player, Card, TEvent, TReturn> eventHandler, Duration duration = Duration.UntilDiscarded) where TEvent : IEffectWithResultEvent<TReturn>
    {
        if (!_eventWithResultHandlers.ContainsKey(typeof(TEvent)))
            _eventWithResultHandlers[typeof(TEvent)] = new();

        object? Handler(Player triggeringPlayer, Card localCard, IEffectWithResultEvent effectEvent)
        {
            return eventHandler(player, localCard, (TEvent)effectEvent);
        }

        _eventWithResultHandlers[typeof(TEvent)].Add((card, player, true, Handler, duration));
    }

    public async Task HandleAsync<TEvent>(IGameHub gameHub, Player player, TEvent theEvent) where TEvent : IEffectEvent
    {
        if (_eventHandlers.ContainsKey(typeof(TEvent)))
        {
            var eventHandlers = _eventHandlers[typeof(TEvent)].Where(x => x.self == (player == x.player)).ToList();
            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.handler(player, eventHandler.card, theEvent);
            }
        }

        if (_asyncEventHandlers.ContainsKey(typeof(TEvent)))
        {
            var eventHandlers = _asyncEventHandlers[typeof(TEvent)].Where(x => x.self == (player == x.player)).ToList();
            foreach (var eventHandler in eventHandlers)
            {
                await eventHandler.handler(gameHub, player, eventHandler.card, theEvent);
            }
        }
    }

    public List<(Card card, TResult result)> HandleWithResult<TEvent, TResult>(Player player, TEvent theEvent) where TEvent : IEffectWithResultEvent<TResult>
    {
        if (!_eventWithResultHandlers.ContainsKey(typeof(TEvent)))
            return new List<(Card, TResult)>();

        var eventHandlers = _eventWithResultHandlers[typeof(TEvent)].Where(x => x.self == (player == x.player)).ToList();

        var results = new List<(Card, TResult)>();
        foreach (var eventHandler in eventHandlers)
        {
            results.Add((eventHandler.card, (TResult)(eventHandler.handler(player, eventHandler.card, theEvent))!));
        }

        return results;
    }

    public EventHandlerHelper<TResult> For<TResult>() => new(this);

    public class EventHandlerHelper<TResult>
    {
        private readonly EventHandler _eventHandler;

        public EventHandlerHelper(EventHandler eventHandler)
        {
            _eventHandler = eventHandler;
        }

        public List<(Card card, TResult result)> Handle<TEvent>(Player player, TEvent theEvent) where TEvent : IEffectWithResultEvent<TResult>
        {
            return _eventHandler.HandleWithResult<TEvent, TResult>(player, theEvent);
        }
    }

    public void CardDiscarded(Card card)
    {
        _eventHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.card == card));
        _asyncEventHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.card == card));
        _eventWithResultHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.card == card));
    }

    public void DurationEnded(Duration duration)
    {
        _eventHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.duration == duration));
        _asyncEventHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.duration == duration));
        _eventWithResultHandlers.Values.ForEach((x, i) => x.RemoveAll(y => y.duration == duration));
    }
}