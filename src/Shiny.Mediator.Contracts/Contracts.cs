namespace Shiny.Mediator;


public interface ICommand;
public interface IEvent;
public interface IStreamRequest<out TResult>; 
public interface IRequest<out TResult>;

/// <summary>
/// This is viewed by replay, cache, and various other services where you can control an entry
/// Simply mark your IRequest or IStreamRequest and provide the necessary key to determine uniqueness
/// </summary>
public interface IRequestKey
{
    string GetKey() => this.ReflectKey();
}