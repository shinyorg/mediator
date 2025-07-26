namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Provides a way to 
/// </summary>
public interface IContractKeyProvider
{
    /// <summary>
    /// Builds a key that can be used for caching/offline/storage unique identification
    /// </summary>
    /// <param name="obj"></param>

    string GetContractKey(object contract);
}