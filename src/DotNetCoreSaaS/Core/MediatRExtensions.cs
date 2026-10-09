using MediatR;

namespace DotNetCoreSaaS.Core;

public static class MediatRExtensions
{
  public static IServiceCollection AddMediatR(
    this IServiceCollection services
  )
  {
    return services
      .AddScoped<IMediator, Mediator>()
      .AddTransient<ServiceFactory>(sp => sp.GetRequiredService!);
  }
}
