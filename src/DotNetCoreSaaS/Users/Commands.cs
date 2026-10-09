using DotNetCoreSaaS.Core;
using Marten;

namespace DotNetCoreSaaS.Users;

public record RegisterUser : ICommand
{
  public string Email { get; init; }
  public string FirstName { get; init; }
  public string LastName { get; init; }
  public string TenantId { get; init; }
  public Guid Id { get; init; }
  public string Sub { get; init; }

  private RegisterUser(
    string email,
    string firstName,
    string lastName,
    string tenantId,
    Guid Id,
    string sub)
  {
    Email = email;
    FirstName = firstName;
    LastName = lastName;
    Id = Id;
    Sub = sub;
  }

  public static RegisterUser Create(
    string email,
    string firstName,
    string lastName,
    string tenantId,
    Guid id,
    string sub)
  {
    return new RegisterUser(email, firstName, lastName, tenantId, id, sub);
  }

  public void Deconstruct(
    out string email,
    out string firstName,
    out string lastName,
    out string tenantId,
    out Guid id,
    out string sub
  ) => (email, firstName, lastName, tenantId, id, sub) = (Email, FirstName, LastName, TenantId, Id, Sub);
}

