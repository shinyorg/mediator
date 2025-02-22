using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public interface IMediator : IRequestExecutor, ICommandExecutor, IEventExecutor
{
}