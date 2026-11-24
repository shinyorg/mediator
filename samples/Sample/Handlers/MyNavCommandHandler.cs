using Sample.Contracts;

namespace Sample.Handlers;


[MediatorSingleton]
public partial class MyNavCommandHandler : ICommandHandler<MyNavCommand>
{
    // AnotherViewModel receives the argument through IQueryAttributable
    [MainThread]
    public Task Handle(MyNavCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Shell.Current.GoToAsync(
            nameof(AnotherPage),
            new Dictionary<string, object> { [AnotherViewModel.ArgKey] = command.Arg ?? "No Argument" }
        );
}
