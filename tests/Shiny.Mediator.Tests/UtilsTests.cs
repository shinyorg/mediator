using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Tests
{
    public class UtilsTests
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
        
        
        [Theory]
        [InlineData(true, true, true, true, true, "contractspecific")]
        [InlineData(false, true, true, true, true, "handlerspecific")]
        [InlineData(false, true, false, true, true, "contractall")]
        [InlineData(false, false, false, true, true, "handlerall")]
        [InlineData(false, false, false, false, true, "all")]
        [InlineData(false, false, false, false, false, null)]
        public void Configuration_ProperOrdering(
            bool contractSpecific, 
            bool contractAll, 
            bool handlerSpecific,
            bool allHandler, 
            bool all, 
            string expectedValue
        )
        {
            var config = new ConfigurationManager();

            if (contractSpecific)
                config["Mediator:Tests:MyContracts.ContractType"] = "contractspecific";
            if (contractAll)
                config["Mediator:Tests:MyContracts.*"] = "contractall";
            if (handlerSpecific)
                config["Mediator:Tests:MyHandlers.HandlerType"] = "handlerspecific";
            if (allHandler)
                config["Mediator:Tests:MyHandlers.*"] = "handlerall";
            if (all)
                config["Mediator:Tests:*"] = "all";

            var section = Utils.GetHandlerSection(
                config, 
                "Tests", 
                new MyContracts.ContractType(),
                new MyHandlers.HandlerType()
            );
            if (expectedValue == null)
            {
                section.ShouldBeNull();
            }
            else
            {
                section.ShouldNotBeNull("Section not found");
                section.Value.ShouldBe(expectedValue);
            }
        }
    }
    
    
    file record MyAttributeRequest : IRequest<string>;
    file class MyAttributeRequestHandler : IRequestHandler<MyAttributeRequest, string>
    {
        [MyAttribute]
        public Task<string> Handle(MyAttributeRequest request, IMediatorContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }



    file record MyAttribute1Command : ICommand;
    file record MyAttribute2Command : ICommand;

    file class MyAttributeCommandHandler : ICommandHandler<MyAttribute1Command>, ICommandHandler<MyAttribute2Command>
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
    file class MyAttributeAttribute : Attribute;
}

namespace MyHandlers
{
    class HandlerType {}
}

namespace MyContracts
{
    class ContractType {}
}