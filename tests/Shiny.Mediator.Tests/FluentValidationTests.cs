using FluentValidation;

namespace Shiny.Mediator.Tests;


public class FluentValidationTests
{
    readonly IMediator mediator;

    public FluentValidationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShinyMediator(cfg => cfg.AddFluentValidation(this.GetType().Assembly), false);
        services.AddSingletonAsImplementedInterfaces<FluentValidationCommandHandler>();
        services.AddSingletonAsImplementedInterfaces<FluentValidationRequestHandler>();
        this.mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task WithValidateResult()
    {
        var response = await this.mediator.Request(new SampleFluentValidationRequest());
        response.Result.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WithoutValidateResult()
    {
        try
        {
            await this.mediator.Send(new SampleFluentValidationCommand());
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
        var response = await this.mediator.Request(new SampleFluentValidationRequest { Name = "Allan", Url = "https://test.com" });
        response.Result.IsValid.ShouldBeTrue();
    }
}

// Sample request/command and validators for FluentValidation
[Validate]
public class SampleFluentValidationRequest : IRequest<ValidateResult>
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

[Validate]
public class SampleFluentValidationCommand : ICommand
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class SampleFluentValidationRequestValidator : AbstractValidator<SampleFluentValidationRequest>
{
    public SampleFluentValidationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Url).Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Invalid URL");
    }
}

public class SampleFluentValidationCommandValidator : AbstractValidator<SampleFluentValidationCommand>
{
    public SampleFluentValidationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Url).Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Invalid URL");
    }
}

public class FluentValidationRequestHandler : IRequestHandler<SampleFluentValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(SampleFluentValidationRequest request, IMediatorContext context, CancellationToken cancellationToken) =>
        Task.FromResult(new ValidateResult(new Dictionary<string, IReadOnlyList<string>>()));
}

public class FluentValidationCommandHandler : ICommandHandler<SampleFluentValidationCommand>
{
    public Task Handle(SampleFluentValidationCommand command, IMediatorContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

