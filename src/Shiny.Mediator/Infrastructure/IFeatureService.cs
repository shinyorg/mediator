namespace Shiny.Mediator.Infrastructure;

public interface IFeatureService
{
    TConfig? GetIfAvailable<TConfig>(object request, object handler) where TConfig : Attribute;
    TConfig? GetFromConfigIfAvailable<TConfig>(object request, object handler);
}