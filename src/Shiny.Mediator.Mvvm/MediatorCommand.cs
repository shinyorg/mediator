using System.Windows.Input;

namespace Shiny.Mediator.Mvvm;

public class MediatorCommand<TRequest>(IMediator mediator) : ICommand
{
    public bool CanExecute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public event EventHandler? CanExecuteChanged;
}

public class MediatorCommand<TRequest, TArg>(IMediator mediator) : ICommand
{
    public bool CanExecute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public event EventHandler? CanExecuteChanged;
}