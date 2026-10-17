using System.Collections.Concurrent;
using System.Reflection;
using kliento.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;

namespace DotNetCoreSaaS.Core.EventSourcing;

public interface IEventBus
{
  Task Publish(
    object @event,
    CancellationToken ct
  );
}

public class EventBus : IEventBus
{
  private readonly IServiceProvider _serviceProvider;
  private readonly Func<IServiceProvider, EventEnvelope?, TracingScope> _createTracingScope;
  private readonly AsyncPolicy _retryPolicy;
  private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();

  public EventBus(
    IServiceProvider serviceProvider,
    Func<IServiceProvider, EventEnvelope?, TracingScope> createTracingScope,
    AsyncPolicy retryPolicy
  )
  {
    _serviceProvider = serviceProvider;
    _createTracingScope = createTracingScope;
    _retryPolicy = retryPolicy;
  }

  private async Task Publish<TEvent>(
    TEvent @event,
    CancellationToken ct
  )
  {
    var eventEnvelope = @event as EventEnvelope;
    using var scope = _serviceProvider.CreateScope();
    using var tracingScope = _createTracingScope(
      scope.ServiceProvider,
      eventEnvelope
    );

    var eventHandlers =
      scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();

    foreach (var eventHandler in eventHandlers)
    {
      await _retryPolicy.ExecuteAsync(
        async token =>
        {
          await eventHandler.Handle(
            @event,
            token
          );
        },
        ct
      );
    }
  }

  public async Task Publish(
    object @event,
    CancellationToken ct
  )
  {
    // if it's an event envelope, publish also just event data
    // thanks to that both handlers with envelope and without will be called
    if (@event is EventEnvelope(var data, _))
      await (Task)GetGenericPublishFor(data)
        .Invoke(
          this,
          new[] { data, ct }
        )!;

    await (Task)GetGenericPublishFor(@event)
      .Invoke(
        this,
        new[] { @event, ct }
      )!;
  }

  private static MethodInfo GetGenericPublishFor(
    object @event
  ) =>
    PublishMethods.GetOrAdd(
      @event.GetType(),
      eventType =>
        typeof(EventBus)
          .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
          .Single(
            m => m.Name == nameof(Publish) && m.GetGenericArguments()
              .Any()
          )
          .MakeGenericMethod(eventType)
    );
}

public static class EventBusExtensions
{
  public static IServiceCollection AddEventBus(
    this IServiceCollection services,
    AsyncPolicy? asyncPolicy = null
  )
  {
    services.AddSingleton(
      sp => new EventBus(
        sp,
        sp.GetRequiredService<ITracingScopeFactory>()
          .CreateTraceScope,
        asyncPolicy ?? Policy.NoOpAsync()
      )
    );
    services
      .TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<EventBus>());

    return services;
  }
}

public static class Config
{
  public static IServiceCollection AddEventHandler<TEvent, TEventHandler>(
    this IServiceCollection services
  )
    where TEventHandler : class, IEventHandler<TEvent>
  {
    return services
      .AddTransient<TEventHandler>()
      .AddTransient<IEventHandler<TEvent>>(sp => sp.GetRequiredService<TEventHandler>());
  }
}
