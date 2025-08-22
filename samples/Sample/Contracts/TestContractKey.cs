namespace Sample.Contracts;

[ContractKey("Test_{Name}_{DoubleValue}_{IntValue}_{NullableIntValue}_{Timestamp:yyyyMMddHHmmss}")]
public partial class TestContractKey
{
    public string Name { get; set; }
    public DateTime? Timestamp { get; set; }
    public double DoubleValue { get; set; }
    public int IntValue { get; set; }
    public int? NullableIntValue { get; set; }
}