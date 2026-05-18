using System.Diagnostics.CodeAnalysis;

namespace Shiny.Mediator;


/// <summary>
/// Reflection helpers used by source generators and runtime registration to identify mediator handler and
/// contract types from a <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns true when <paramref name="type"/> implements <see cref="IRequestHandler{TRequest,TResult}"/>.
    /// </summary>
    public static bool IsRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> implements <see cref="ICommandHandler{TCommand}"/>.
    /// </summary>
    public static bool IsCommandHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> implements <see cref="IEventHandler{TEvent}"/>.
    /// </summary>
    public static bool IsEventHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> implements <see cref="IStreamRequestHandler{TRequest,TResult}"/>.
    /// </summary>
    public static bool IsStreamRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> is a command contract (implements <see cref="ICommand"/>).
    /// </summary>
    public static bool IsCommandContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(ICommand)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> is a request contract (implements <see cref="IRequest{TResult}"/>).
    /// </summary>
    public static bool IsRequestContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IRequest<>)
        );


    /// <summary>
    /// Returns true when <paramref name="type"/> is an event contract (implements <see cref="IEvent"/>).
    /// </summary>
    public static bool IsEventContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x =>
            x.IsGenericType &&
            x.GetGenericTypeDefinition() == typeof(IEvent)
        );
}