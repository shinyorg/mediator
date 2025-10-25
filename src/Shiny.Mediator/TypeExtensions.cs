using System.Diagnostics.CodeAnalysis;

namespace Shiny.Mediator;


public static class TypeExtensions
{
    /// <summary>
    /// Checks if current type is a request handler
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
        );
    
    
    /// <summary>
    /// Checks if current type is a command handler
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCommandHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
        );
    

    /// <summary>
    /// Checks if current type is an event handler
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsEventHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
        );
    
    
    /// <summary>
    /// Checks if current type is a stream request handler
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsStreamRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>)
        );
    
    
    /// <summary>
    /// Checks if current type is a command message
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCommandContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(ICommand)
        );
    
    
    /// <summary>
    /// Checks if current type is a request message
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsRequestContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IRequest<>)
        );


    /// <summary>
    /// Checks if current type is an event message
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsEventContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IEvent)
        );
}