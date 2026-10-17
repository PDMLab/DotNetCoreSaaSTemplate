using Marten.Schema.Identity;

namespace DotNetCoreSaaS.Core.EventSourcing;

public interface IIdGenerator
{
  Guid New();
}

public class MartenIdGenerator : IIdGenerator
{
  public Guid New() => CombGuidIdGeneration.NewGuid();
}
