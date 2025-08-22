namespace Sample.Contracts;

[ContractKey("Test_{Name}_{Timestamp:yyyyMMddHHmmss}")]
public partial class TestContractKey
{
    public string Name { get; set; }
    public DateTime? Timestamp { get; set; }
}