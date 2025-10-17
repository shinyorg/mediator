using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Tests
{
    public class UtilsTests
    {
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
    
    
}

namespace MyHandlers
{
    class HandlerType {}
}

namespace MyContracts
{
    class ContractType {}
}