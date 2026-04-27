using Shiny.Mediator;

namespace Sample.CopilotConsole;

[MediatorScoped]
public class GetWeatherRequestHandler : IRequestHandler<GetWeatherRequest, WeatherResult>
{
    static readonly string[] Conditions = ["Sunny", "Cloudy", "Rainy", "Snowy", "Windy", "Partly Cloudy"];

    public Task<WeatherResult> Handle(
        GetWeatherRequest request, IMediatorContext context, CancellationToken ct)
    {
        // Simulated weather data
        var random = new Random(request.City.GetHashCode());
        var tempC = random.Next(-10, 40);
        var temp = request.Unit.Equals("fahrenheit", StringComparison.OrdinalIgnoreCase)
            ? tempC * 9.0 / 5.0 + 32
            : tempC;
        var condition = Conditions[random.Next(Conditions.Length)];

        return Task.FromResult(new WeatherResult(request.City, temp, request.Unit, condition));
    }
}

[MediatorScoped]
public class GetDateTimeRequestHandler : IRequestHandler<GetDateTimeRequest, DateTimeResult>
{
    public Task<DateTimeResult> Handle(
        GetDateTimeRequest request, IMediatorContext context, CancellationToken ct)
    {
        var tz = request.Timezone;
        DateTimeOffset now;
        string tzName;

        if (string.IsNullOrWhiteSpace(tz))
        {
            now = DateTimeOffset.UtcNow;
            tzName = "UTC";
        }
        else
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
            now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzInfo);
            tzName = tzInfo.DisplayName;
        }

        return Task.FromResult(new DateTimeResult(now.ToString("yyyy-MM-dd HH:mm:ss zzz"), tzName));
    }
}

[MediatorScoped]
public class CalculateRequestHandler : IRequestHandler<CalculateRequest, CalculateResult>
{
    public Task<CalculateResult> Handle(
        CalculateRequest request, IMediatorContext context, CancellationToken ct)
    {
        var (a, b, op) = request;
        var (result, expr) = op.ToLowerInvariant() switch
        {
            "add" => (a + b, $"{a} + {b} = {a + b}"),
            "subtract" => (a - b, $"{a} - {b} = {a - b}"),
            "multiply" => (a * b, $"{a} * {b} = {a * b}"),
            "divide" when b != 0 => (a / b, $"{a} / {b} = {a / b}"),
            "divide" => (double.NaN, $"{a} / {b} = Error: Division by zero"),
            _ => throw new ArgumentException($"Unknown operation: {op}")
        };

        return Task.FromResult(new CalculateResult(result, expr));
    }
}
