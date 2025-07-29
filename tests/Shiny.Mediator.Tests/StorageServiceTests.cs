using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class StorageServiceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Issue_40_NoDeadlock()
    {
        var storage = new TestStorageService(
            new SysTextJsonSerializerService(),
            TestHelpers.CreateLogger<TestStorageService>(output)
        );

        var tasks = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            if (i % 2 == 0)
            {
                tasks.Add(storage.Set("test", $"key{i}", new TestClass { TestProperty = "Hello World" }, CancellationToken.None));
            }
            else
            {
                tasks.Add(storage.Clear("test", CancellationToken.None));
            }
        }
        await Task.WhenAll(tasks);        
    }
}

file class TestClass
{
    public string TestProperty { get; set; }
}

file class TestStorageService(
    ISerializerService serializer, 
    ILogger<TestStorageService> logger
) : AbstractFileStorageService(serializer, logger)
{
    protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(fileName, content, cancellationToken);

    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        if (!File.Exists(fileName))
            return null;
        
        var result = await File.ReadAllTextAsync(fileName, cancellationToken);
        return result;
    }

    protected override Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);
        
        return Task.CompletedTask;
    }
}