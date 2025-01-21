using System.ComponentModel.DataAnnotations;

namespace Shiny.Mediator.Tests;


public class DataAnnotationsTests
{
    readonly IMediator mediator;
    
    
    public DataAnnotationsTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShinyMediator(cfg => cfg.AddDataAnnotations(), false);
        services.AddSingletonAsImplementedInterfaces<ValidationCommandHandler>();
        services.AddSingletonAsImplementedInterfaces<ValidationRequestHandler>();
        this.mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();    
    }
    
    
    [Fact]
    public async Task WithValidateResult()
    {
        var result = await this.mediator.Request(new ValidationRequest());
        result.Errors.Count.ShouldBe(2);
    }
    
    
    [Fact]
    public async Task WithoutValidateResult()
    {
        try
        {
            await this.mediator.Send(new ValidationCommand());
            Assert.Fail("Should have thrown");
        }
        catch (ValidateException ex)
        {
            ex.Result.Errors.Count.ShouldBe(2);
        }
    }
    

    [Fact]
    public async Task Success()
    {
        var result = await this.mediator.Request(new ValidationRequest { Name = "Allan", Url = "https://test.com" });
        result.IsValid.ShouldBeTrue();
    }
}


[Validate]
public class ValidationCommand : ICommand
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

public class ValidationCommandHandler : ICommandHandler<ValidationCommand>
{
    public Task Handle(ValidationCommand request, CommandContext<ValidationCommand> context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Never should have gotten here");
    }
}

public class ValidationRequestHandler : IRequestHandler<ValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(ValidationRequest request, RequestContext<ValidationRequest> context, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidateResult.Success);
    }
}