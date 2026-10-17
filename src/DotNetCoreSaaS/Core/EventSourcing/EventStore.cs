namespace DotNetCoreSaaS.Core.EventSourcing;

public static class EventStore
{
  public static string GetSubscriptionEventStoreId(
    SubscriptionId subscriptionId
  )
  {
    return $"kliento_sub_{subscriptionId.Value.ToString().Replace("-", "_")}";
  }

  public static string GetDefaultEventStoreId(
    EventStoreConfiguration? configuration
  )
  {
    return string.IsNullOrEmpty(configuration?.DefaultEventstoreId)
      ? Constants.DefaultEventStore
      : configuration.DefaultEventstoreId;
  }
}

public static class Constants
{
  public const string DefaultEventStore = "kliento";
}
