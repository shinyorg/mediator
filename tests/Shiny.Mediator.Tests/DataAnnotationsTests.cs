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
        services.AddSingletonAsImplementedInterfaces<RangeValidationRequestHandler>();
        services.AddSingletonAsImplementedInterfaces<RangeValidationCommandHandler>();
        services.AddSingletonAsImplementedInterfaces<ObjectLevelValidationRequestHandler>();
        services.AddSingletonAsImplementedInterfaces<ObjectLevelValidationCommandHandler>();
        this.mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();    
    }
    
    
    [Fact]
    public async Task WithValidateResult()
    {
        var response = await this.mediator.Request(new ValidationRequest());
        response.Result.Errors.Count.ShouldBe(2);
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
    public async Task NonRequiredAttributes_AreValidated_Request()
    {
        var response = await this.mediator.Request(new RangeValidationRequest { Quantity = 500, Url = "not a url" });
        response.Result.IsValid.ShouldBeFalse();
        response.Result.Errors.Keys.ShouldBe(["Quantity", "Url"], ignoreOrder: true);
    }


    [Fact]
    public async Task NonRequiredAttributes_AreValidated_Command()
    {
        var ex = await Should.ThrowAsync<ValidateException>(() => this.mediator.Send(new RangeValidationCommand { Quantity = 0 }));
        ex.Result.Errors.Keys.ShouldBe(["Quantity"]);
    }


    [Fact]
    public async Task NonRequiredAttributes_Valid()
    {
        var response = await this.mediator.Request(new RangeValidationRequest { Quantity = 5, Url = "https://test.com" });
        response.Result.IsValid.ShouldBeTrue();
    }


    [Fact]
    public async Task ValidatableObject_WithoutMemberNames_IsNotDropped_Request()
    {
        var response = await this.mediator.Request(new ObjectLevelValidationRequest { Start = 10, End = 1 });
        response.Result.IsValid.ShouldBeFalse();
        response.Result.Errors[String.Empty].ShouldBe(["Start must be before End"]);
    }


    [Fact]
    public async Task ValidatableObject_WithMemberNames_KeyedByMember()
    {
        var response = await this.mediator.Request(new ObjectLevelValidationRequest { Start = 1, End = 2, Code = "bad" });
        response.Result.Errors.Keys.ShouldBe(["Code"]);
    }


    [Fact]
    public async Task ClassLevelAttribute_WithoutMemberNames_IsNotDropped_Command()
    {
        var ex = await Should.ThrowAsync<ValidateException>(() => this.mediator.Send(new ObjectLevelValidationCommand()));
        ex.Result.Errors[String.Empty].ShouldBe(["Command is never valid"]);
    }


    [Fact]
    public async Task ValidatableObject_Valid()
    {
        var response = await this.mediator.Request(new ObjectLevelValidationRequest { Start = 1, End = 2 });
        response.Result.IsValid.ShouldBeTrue();
    }


    [Fact]
    public async Task Success()
    {
        var response = await this.mediator.Request(new ValidationRequest { Name = "Allan", Url = "https://test.com" });
        response.Result.IsValid.ShouldBeTrue();
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
    public Task Handle(ValidationCommand request, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Never should have gotten here");
    }
}

public class ValidationRequestHandler : IRequestHandler<ValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(ValidationRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidateResult.Success);
    }
}

[Validate]
public class RangeValidationRequest : IRequest<ValidateResult>
{
    [Range(1, 10)] public int Quantity { get; set; }
    [Url] public string? Url { get; set; }
}

[Validate]
public class RangeValidationCommand : ICommand
{
    [Range(1, 10)] public int Quantity { get; set; }
}

public class RangeValidationRequestHandler : IRequestHandler<RangeValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(RangeValidationRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(ValidateResult.Success);
}

public class RangeValidationCommandHandler : ICommandHandler<RangeValidationCommand>
{
    public Task Handle(RangeValidationCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Never should have gotten here");
}


[Validate]
public class ObjectLevelValidationRequest : IRequest<ValidateResult>, IValidatableObject
{
    public int Start { get; set; }
    public int End { get; set; }
    public string? Code { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Start >= this.End)
            yield return new ValidationResult("Start must be before End"); // no member names

        if (this.Code == "bad")
            yield return new ValidationResult("Code is bad", [nameof(this.Code)]);
    }
}

public class NeverValidAttribute() : ValidationAttribute("Command is never valid")
{
    public override bool IsValid(object? value) => false;
}

[Validate]
[NeverValid]
public class ObjectLevelValidationCommand : ICommand;

public class ObjectLevelValidationRequestHandler : IRequestHandler<ObjectLevelValidationRequest, ValidateResult>
{
    public Task<ValidateResult> Handle(ObjectLevelValidationRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(ValidateResult.Success);
}

public class ObjectLevelValidationCommandHandler : ICommandHandler<ObjectLevelValidationCommand>
{
    public Task Handle(ObjectLevelValidationCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Never should have gotten here");
}
