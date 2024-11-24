namespace Shiny.Mediator.Server.Client;

static class Extensions
{
    public static Type? GetRequestContract(this Type implType)
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
        return interfaceType?.GetGenericArguments().FirstOrDefault();
    }

    public static Type? GetEventContract(this Type implType)
    {
        var interfaceType = implType
            .GetInterfaces()
            .FirstOrDefault(x => 
                x.IsGenericType && 
                x.GetGenericTypeDefinition() == typeof(IEventHandler<>)
            );

        // Return the generic argument type, or null if not implemented
        return interfaceType?.GetGenericArguments().FirstOrDefault();
    }


    public static bool IsContractType(this Type type)
    {
        // TODO: cannot be multiple contract types for server
        if (type.IsAssignableTo(typeof(IEvent)))
            return true;

        if (type.IsAssignableTo(typeof(IRequest)))
            return true;

        if (type.IsAssignableTo(typeof(IRequest<>)))
            return true;

        return false;
    }
}