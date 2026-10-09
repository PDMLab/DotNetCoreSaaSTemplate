using System.Data.Common;
using Marten;

namespace DotNetCoreSaaS.Core;

public static class EventStoreConfigurationExtensions
{
  public static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration,
    ILogger logger)
  {
    logger.LogInformation("Connecting Marten Event Store");
    var connectionString = configuration.GetConnectionString("EventStore");
    
    services.AddMarten(options =>
    {
      options.Connection(connectionString);
    });
  
    logger.LogInformation("Connected Marten Event Store");
    return services;
  }
}

public class EventStoreConfiguration
{
  public string ConnectionString { get; set; }
}

public class EventStore
{
}
