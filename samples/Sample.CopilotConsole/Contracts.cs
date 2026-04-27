using System.ComponentModel;
using Shiny.Mediator;

namespace Sample.CopilotConsole;

[Description("Get the current weather forecast for a given city")]
public record GetWeatherRequest(
    [property: Description("The city name to get weather for")]
    string City,

    [property: Description("Temperature unit: 'celsius' or 'fahrenheit'")]
    string Unit = "celsius"
) : IRequest<WeatherResult>;

public record WeatherResult(string City, double Temperature, string Unit, string Condition);


[Description("Get the current date and time, optionally for a specific timezone")]
public record GetDateTimeRequest(
    [property: Description("IANA timezone name (e.g. 'America/New_York'). If omitted, returns UTC.")]
    string? Timezone = null
) : IRequest<DateTimeResult>;

public record DateTimeResult(string DateTime, string Timezone);


[Description("Perform a math calculation with two numbers")]
public record CalculateRequest(
    [property: Description("First operand")]
    double A,

    [property: Description("Second operand")]
    double B,

    [property: Description("The operation to perform: add, subtract, multiply, or divide")]
    string Operation
) : IRequest<CalculateResult>;

public record CalculateResult(double Result, string Expression);
