using DotNetCoreSaaS.Core.Tracing;
using EventSourcing;
using kliento.Core;
using Marten;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Weasel.Core;

namespace DotNetCoreSaaS.Core.EventSourcing;

public class EventStoreConfiguration
{
  private const string DefaultSchema = "public";

  public string ConnectionString { get; set; } = default!;

  public string WriteModelSchema { get; set; } = DefaultSchema;
  public string ReadModelSchema { get; set; } = DefaultSchema;

  public bool ShouldRecreateDatabase { get; set; }

  // public bool UseMetadata = true;
  public string DefaultEventstoreId { get; set; } = "kliento";
}

public static class MartenConfigExtensions
{
  private const string DefaultConfigKey = "EventStore";

  public static IServiceCollection AddMarten(
    this IServiceCollection services,
    IConfiguration config,
    Action<StoreOptions>? configureOptions = null,
    string configKey = DefaultConfigKey
  )
  {
    var martenConfig = config.GetSection(configKey)
      .Get<EventStoreConfiguration>();

    services
      .AddSingleton(martenConfig)
      .AddScoped<IIdGenerator, MartenIdGenerator>()
      .AddMarten(
        _ => SetStoreOptions(
          martenConfig,
          configureOptions
        )
      );

    return services;
  }

  private static StoreOptions SetStoreOptions(
    EventStoreConfiguration eventStoreConfiguration,
    Action<StoreOptions>? configureOptions = null
  )
  {
    var options = new StoreOptions();
    options.MultiTenantedWithSingleServer(eventStoreConfiguration.ConnectionString);
    options.AutoCreateSchemaObjects = AutoCreate.All;

    var schemaName = Environment.GetEnvironmentVariable("SchemaName");
    options.Events.DatabaseSchemaName = schemaName ?? eventStoreConfiguration.WriteModelSchema;
    options.DatabaseSchemaName = schemaName ?? eventStoreConfiguration.ReadModelSchema;

    options.UseDefaultSerialization(
      EnumStorage.AsString,
      nonPublicMembersStorage: NonPublicMembersStorage.All
    );

    configureOptions?.Invoke(options);

    // if (eventStoreConfiguration.UseMetadata)
    // {
    //   options.Events.MetadataConfig.CausationIdEnabled = true;
    //   options.Events.MetadataConfig.CorrelationIdEnabled = true;
    // }

    return options;
  }

  public static IServiceCollection AddMediatR(
    this IServiceCollection services
  )
  {
    return services
      .AddScoped<IMediator, Mediator>()
      .AddTransient<ServiceFactory>(sp => sp.GetRequiredService);
  }

  public static IServiceCollection AddTracing(
    this IServiceCollection services
  )
  {
    services.TryAddSingleton<ICorrelationIdFactory, GuidCorrelationIdFactory>();
    services.TryAddSingleton<ICausationIdFactory, GuidCausationIdFactory>();
    services.TryAddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
    services.TryAddScoped<ICausationIdProvider, CausationIdProvider>();
    services.TryAddScoped<ITraceMetadataProvider, TraceMetadataProvider>();
    services.TryAddSingleton<ITracingScopeFactory, TracingScopeFactory>();

    services.TryAddScoped<Func<IServiceProvider, TraceMetadata?, TracingScope>>(
      sp =>
        (
            scopedServiceProvider,
            traceMetadata
          ) =>
          sp.GetRequiredService<ITracingScopeFactory>()
            .CreateTraceScope(
              scopedServiceProvider,
              traceMetadata
            )
    );

    services.TryAddScoped<Func<IServiceProvider, EventEnvelope?, TracingScope>>(
      sp =>
      (
        scopedServiceProvider,
        eventEnvelope
      ) => sp.GetRequiredService<ITracingScopeFactory>()
        .CreateTraceScope(
          scopedServiceProvider,
          eventEnvelope
        )
    );

    return services;
  }

  public static IServiceCollection AddCommandHandler<TCommand, TCommandHandler>(
    this IServiceCollection services
  )
    where TCommand : ICommand
    where TCommandHandler : class, ICommandHandler<TCommand>
  {
    return services.AddTransient<TCommandHandler>()
      .AddTransient<IRequestHandler<TCommand, Unit>>(sp => sp.GetRequiredService<TCommandHandler>())
      .AddTransient<ICommandHandler<TCommand>>(sp => sp.GetRequiredService<TCommandHandler>());
  }
}
