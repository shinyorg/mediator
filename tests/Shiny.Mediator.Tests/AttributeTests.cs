namespace Shiny.Mediator.Tests;

public class AttributeTests
{
    [Fact]
    public void FoundAttributeOnContract()
    {
        new MyAttributeRequestHandler()
            .GetHandlerHandleMethodAttribute<MyAttributeRequest, string, MyAttributeAttribute>()
            .ShouldNotBeNull();
    }
    


    [Fact]
    public void FoundAttributeOnlyOnOneHandleMethod()
    {
        var handler = new MyAttributeCommandHandler();
        handler    
            .GetHandlerHandleMethodAttribute<MyAttribute1Command, MyAttributeAttribute>()
            .ShouldNotBeNull();
        
        handler    
            .GetHandlerHandleMethodAttribute<MyAttribute2Command, MyAttributeAttribute>()
            .ShouldBeNull();
    }
}


public record MyAttributeRequest : IRequest<string>;
public class MyAttributeRequestHandler : IRequestHandler<MyAttributeRequest, string>
{
    [MyAttribute]
    public Task<string> Handle(MyAttributeRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}



public record MyAttribute1Command : ICommand;
public record MyAttribute2Command : ICommand;

public class MyAttributeCommandHandler : ICommandHandler<MyAttribute1Command>, ICommandHandler<MyAttribute2Command>
{
    [MyAttribute]
    public Task Handle(MyAttribute1Command command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public Task Handle(MyAttribute2Command command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MyAttributeAttribute : Attribute;
