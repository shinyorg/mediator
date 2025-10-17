using System.Diagnostics.CodeAnalysis;

namespace Shiny.Mediator;

public static class TypeExtensions
{
    public static bool IsRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
        );
    
    
    public static bool IsCommandHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
        );

    public static bool IsEventHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
        );
    
    
    public static bool IsStreamRequestHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>)
        );
    
    
    public static bool IsCommandContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(ICommand)
        );
    
    
    public static bool IsRequestContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IRequest<>)
        );


    public static bool IsEventContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) => type
        .GetInterfaces()
        .Any(x => 
            x.IsGenericType && 
            x.GetGenericTypeDefinition() == typeof(IEvent)
        );

    
    // public static ContractType? GetContractType(this Type type)
    // {
    //     // TODO: cannot be multiple contract types for server
    //     if (type.IsCommandContract())
    //         return ContractType.Command;
    //
    //     if (type.IsRequestContract())
    //         return ContractType.Request;
    //     
    //     if (type.IsEventContract())
    //         return ContractType.Event;
    //     
    //     return null;
    // }    
}