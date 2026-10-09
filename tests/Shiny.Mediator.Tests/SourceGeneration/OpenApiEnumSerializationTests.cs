using System.Text.Json;

namespace Shiny.Mediator.Tests.SourceGeneration;


/// <summary>
/// Numeric OpenAPI enums keep their names (x-enumNames), flags (x-enumFlags) and travel as numbers
/// on the wire through both the reflection path and the generated per-type converters; decimal
/// formats map to System.Decimal.
/// </summary>
public class OpenApiEnumSerializationTests
{
    const string Json = """
        {"orderId":"6f1f0b5e-6d4e-4c6b-9f2a-0d1f6f3c2a11","status":2,"rights":5,"priority":20,"totalGross":12.5,"discountRate":null,"ratio":0.25}
        """;

    [Fact]
    public void Enum_Members_Use_Document_Names_And_Flags()
    {
        ((int)EnumsApi.OrderStatus.Closed).ShouldBe(2);
        Enum.GetNames<EnumsApi.OrderStatus>().ShouldBe(["Open", "Closed", "Voided"]);
        typeof(EnumsApi.StaffRight).IsDefined(typeof(FlagsAttribute), false).ShouldBeTrue();
        (EnumsApi.StaffRight.Sell | EnumsApi.StaffRight.Cashbook).ShouldBe((EnumsApi.StaffRight)5);
        Enum.GetNames<EnumsApi.Priority>().ShouldBe(["Value10", "Value20"]);
        typeof(EnumsApi.OrderData).GetProperty("TotalGross")!.PropertyType.ShouldBe(typeof(decimal));
        typeof(EnumsApi.OrderData).GetProperty("DiscountRate")!.PropertyType.ShouldBe(typeof(decimal?));
        typeof(EnumsApi.OrderData).GetProperty("Ratio")!.PropertyType.ShouldBe(typeof(double));
    }

    [Fact]
    public void Numeric_Enums_Roundtrip_As_Numbers()
    {
        var obj = JsonSerializer.Deserialize<EnumsApi.OrderData>(Json)!;
        obj.Status.ShouldBe(EnumsApi.OrderStatus.Closed);
        obj.Rights.ShouldBe(EnumsApi.StaffRight.Sell | EnumsApi.StaffRight.Cashbook);
        obj.Priority.ShouldBe(EnumsApi.Priority.Value20);
        obj.TotalGross.ShouldBe(12.5m);
        obj.DiscountRate.ShouldBeNull();

        var json = JsonSerializer.Serialize(obj);
        json.ShouldContain("\"status\":2");
        json.ShouldContain("\"rights\":5");
        json.ShouldContain("\"priority\":20");
        json.ShouldContain("\"totalGross\":12.5");
    }
}
