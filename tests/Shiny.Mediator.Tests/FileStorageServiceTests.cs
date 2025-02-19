using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;

namespace Shiny.Mediator.Tests;


public class FileStorageServiceTests
{
    
}

public class TestFileStorageService(ILogger logger) : AbstractFileStorageService(new SysTextJsonSerializerService(), logger)
{
    protected override Task WriteFile(string fileName, string content)
    {
        throw new NotImplementedException();
    }

    protected override Task<string?> ReadFile(string fileName)
    {
        throw new NotImplementedException();
    }

    protected override Task DeleteFile(string fileName)
    {
        throw new NotImplementedException();
    }
}