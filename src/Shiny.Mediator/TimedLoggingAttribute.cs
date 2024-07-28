namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TimedLoggingAttribute(double errorThresholdMillis) : Attribute
{
    public double ErrorThresholdMillis => errorThresholdMillis;
}
