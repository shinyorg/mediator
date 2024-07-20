using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Tests;


public class DataAnnotationsTests
{
    readonly IMediator mediator;
    
    
    public DataAnnotationsTests()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(cfg => cfg.AddDataAnnotations());
        services.AddSingletonAsImplementedInterfaces<ValidationRequestNoResponseHandler>();
        services.AddSingletonAsImplementedInterfaces<ValidationRequestHandler>();
        this.mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();    
    }
    
    
    [Fact]
    public async Task WithValidateResult()
    {
        var result = await this.mediator.Request(new ValidationRequest());
        result.Errors.Count.Should().Be(2);
    }
    
    
    [Fact]
    public async Task WithoutValidateResult()
    {
        try
        {
            await this.mediator.Send(new ValidationRequestNoResponse());
            Assert.Fail("Should have thrown");
        }
        catch (ValidateException ex)
        {
            ex.Result.Errors.Count.Should().Be(2);
        }
    }
    

    [Fact]
    public async Task Success()
    {
        var result = await this.mediator.Request(new ValidationRequest { Name = "Allan", Url = "https://test.com" });
        result.IsValid.Should().BeTrue();
    }
}


[Validate]
public class ValidationRequestNoResponse : IRequest
{
    [Required] public string? Name { get; set; }
    [Required][Url] public string? Url { get; set; }
}

[Validate]
public class ValidationRequest : IRequest<ValidateResult>
{
    [Required] public string? Name { get; set; }
    [Required][Url] public string? Url { get; set; }
}

public class ValidationRequestNoResponseHandler : IRequestHandler<ValidationRequestNoResponse>
{
    public Task Handle(ValidationRequestNoResponse request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Never should have gotten here");
    }
}

public class ValidationRequestHandler : IRequestHandler<ValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(ValidationRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidateResult.Success);
    }
}