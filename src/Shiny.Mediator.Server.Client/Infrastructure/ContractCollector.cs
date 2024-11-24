using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Server.Client.Infrastructure;

public interface IContractCollector
{
    IEnumerable<Type> Collect();
}


public class ContractCollector(IServiceCollection services) : IContractCollector
{
    public IEnumerable<Type> Collect()
        => this.InternalCollect().ToList().Distinct();


    IEnumerable<Type> InternalCollect()
    {
        // TODO: type isevent or isrequest
        foreach (var service in services)
        {
            if (service.ImplementationType != null)
            {
                // what about generic implementors - we also only want remote services here
                var requestContract = service.ImplementationType.GetRequestContract();
                var eventContract = service.ImplementationType.GetEventContract();
                
                if (requestContract != null)
                    yield return requestContract;
                
                if (eventContract != null)
                    yield return eventContract;
            }
        }
    }
}