namespace DotNetCoreSaaS.Contracts;

public record SubscriptionId : StronglyTypedId<Guid>
{
  public SubscriptionId(
    Guid value
  ) : base(value)
  {
    if (value == Guid.Empty) throw new ArgumentException(nameof(value));
  }

  public static SubscriptionId FromOrganizationId(
    OrganizationId organizationId
  )
  {
    return new SubscriptionId(organizationId.Value);
  }
}
