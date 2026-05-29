using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// Registration helpers for wiring the TickerQ-backed <see cref="ICommandScheduler"/> into Shiny Mediator.
/// </summary>
public static class TickerQRegistrationExtensions
{
    extension(ShinyMediatorBuilder mediatorBuilder)
    {
        /// <summary>
        /// Registers <see cref="TickerQCommandScheduler"/> as the <see cref="ICommandScheduler"/> and installs the
        /// scheduled-command middleware, so commands with a future due-date are persisted and dispatched by TickerQ
        /// instead of running immediately. TickerQ must be configured separately via <c>services.AddTickerQ(...)</c>
        /// (with an operational store) and <c>app.UseTickerQ()</c>.
        /// </summary>
        public ShinyMediatorBuilder AddTickerQCommandScheduling()
            => mediatorBuilder.AddCommandScheduling<TickerQCommandScheduler>();
    }
}
