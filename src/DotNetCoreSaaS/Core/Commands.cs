using MediatR;

namespace DotNetCoreSaaS.Core;

public interface ICommand : IRequest
{
}

public interface ICommandBus
{
  Task Send<TCommand>(TCommand command) where TCommand : ICommand;
}

public class CommandBus : ICommandBus
{
  private readonly IMediator mediator;

  public CommandBus(IMediator mediator)
  {
    this.mediator = mediator;
  }

  public Task Send<TCommand>(TCommand command) where TCommand : ICommand
  {
    return mediator.Send(command);
  }
}

public interface ICommandHandler<in T> : IRequestHandler<T>
  where T : ICommand
{
}
