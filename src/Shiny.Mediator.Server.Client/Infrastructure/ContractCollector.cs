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
                // TODO: must exclude all shiny internal handlers (obviously & especially RemoteRequestHandler)
                // TODO: must match to APP specific handler in scope
                // what about generic implementors - we also only want remote services here
                var requestContract = service.ImplementationType.GetServerRequestContract();
                var eventContract = service.ImplementationType.GetServerEventContract();
                
                if (requestContract != null)
                    yield return requestContract;
                
                if (eventContract != null)
                    yield return eventContract;
            }
        }
    }
}