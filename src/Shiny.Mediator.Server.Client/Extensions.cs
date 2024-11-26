namespace Shiny.Mediator.Server.Client;

static class Extensions
{
    public static Type? GetServerRequestContract(this Type implType)
    {
        var interfaceType = implType
            .GetInterfaces()
            .FirstOrDefault(x => 
                x.IsGenericType && (
                    x.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                    x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                )
            );

        // Return the generic argument type, or null if not implemented
        var type = interfaceType?.GetGenericArguments().FirstOrDefault();
        if (type == null)
            return null;

        if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IServerRequest<>)))
            return null;
        
        return type;
    }

    public static Type? GetServerEventContract(this Type implType)
    {
        var interfaceType = implType
            .GetInterfaces()
            .FirstOrDefault(x => 
                x.IsGenericType && 
                x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
            );

        // Return the generic argument type, or null if not implemented
        var type = interfaceType?.GetGenericArguments().FirstOrDefault();
        if (type?.IsAssignableTo(typeof(IServerEvent)) ?? false)
            return type;
        
        return null;
    }


    public static bool IsContractType(this Type type)
    {
        // TODO: cannot be multiple contract types for server
        if (type.IsAssignableTo(typeof(IServerEvent)))
            return true;

        if (type.IsAssignableTo(typeof(IServerRequest<>)))
            return true;

        return false;
    }
}