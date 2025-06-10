using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.AspNet.SourceGenerators;

namespace Shiny.Mediator.Tests;

public class MediatorEndpointSourceGeneratorTests
{
    [Fact]
    public Task Basic_RequestHandler_GeneratesCorrectEndpoint()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UserResponse(string Name);

public class GetUserHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));
}";

        var generatedFiles = RunGeneratorAndGetFiles(source);
        return Verify(generatedFiles);
    }

    [Fact]
    public Task Basic_CommandHandler_GeneratesCorrectEndpoint()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record CreateUserCommand(string Name) : ICommand;

public class CreateUserHandler : ICommandHandler<CreateUserCommand>
{
    [MediatorHttpPost(""CreateUser"", ""/users"")]
    public Task Handle(CreateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}";

        var generatedFiles = RunGeneratorAndGetFiles(source);
        return Verify(generatedFiles);
    }

    [Fact]
    public Task MixedHandler_UsesCorrectParameterTypes()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record CreateUserCommand(string Name) : ICommand;
public record UserResponse(string Name);

public class MixedHandler : IRequestHandler<GetUserRequest, UserResponse>, ICommandHandler<CreateUserCommand>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));

    [MediatorHttpPost(""CreateUser"", ""/users"")]
    public Task Handle(CreateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}";

        var generatedFiles = RunGeneratorAndGetFiles(source);
        return Verify(generatedFiles);
    }

    [Fact]
    public Task GroupedEndpoints_GeneratesCorrectOutput()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record CreateUserCommand(string Name) : ICommand;
public record UserResponse(string Name);

[MediatorHttpGroup(""/api"")]
public class GroupedHandler : IRequestHandler<GetUserRequest, UserResponse>, ICommandHandler<CreateUserCommand>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));

    [MediatorHttpPost(""CreateUser"", ""/users"")]
    public Task Handle(CreateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    [Fact]
    public Task MultipleHttpMethods_GeneratesCorrectEndpoints()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UpdateUserCommand(int Id, string Name) : ICommand;
public record DeleteUserCommand(int Id) : ICommand;
public record UserResponse(string Name);

public class UserHandler : IRequestHandler<GetUserRequest, UserResponse>, ICommandHandler<UpdateUserCommand>, ICommandHandler<DeleteUserCommand>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));

    [MediatorHttpPut(""UpdateUser"", ""/users/{id}"")]
    public Task Handle(UpdateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    [MediatorHttpDelete(""DeleteUser"", ""/users/{id}"")]
    public Task Handle(DeleteUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    [Fact]
    public Task AttributeProperties_GeneratesCorrectConfiguration()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UserResponse(string Name);

public class UserHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"", 
        RequiresAuthorization = true,
        AuthorizationPolicies = new[] { ""AdminPolicy"" },
        DisplayName = ""Get User by ID"",
        Summary = ""Retrieves a user"",
        Description = ""Gets a user by their unique identifier"",
        Tags = new[] { ""Users"", ""Admin"" },
        AllowAnonymous = false,
        CachePolicy = ""DefaultCache"",
        CorsPolicy = ""DefaultCors"",
        RateLimitingPolicy = ""DefaultRateLimit"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    [Fact]
    public Task GroupAttributeProperties_GeneratesCorrectConfiguration()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UserResponse(string Name);

[MediatorHttpGroup(
    ""/api/v1"",
    RequiresAuthorization = true,
    AuthorizationPolicies = new[] { ""ApiPolicy"" },
    Tags = new[] { ""V1"", ""API"" },
    CachePolicy = ""ApiCache"",
    CorsPolicy = ""ApiCors"",
    RateLimitingPolicy = ""ApiRateLimit""
)]
public class GroupedUserHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    [Fact]
    public Task NoHttpAttributes_GeneratesNothing()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UserResponse(string Name);

public class UserHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    [Fact]
    public Task MultipleAttributes_OnSameMethod_GeneratesMultipleEndpoints()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test;

public record GetUserRequest(int Id) : IRequest<UserResponse>;
public record UserResponse(string Name);

public class UserHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    [MediatorHttpGet(""GetUser"", ""/users/{id}"")]
    [MediatorHttpGet(""GetUserAlt"", ""/user/{id}"")]
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(""Test""));
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }
    

    [Fact]
    public Task ComplexTypes_GeneratesCorrectParameterTypes()
    {
        var source = @"
using Shiny.Mediator;
using System.Threading;

namespace Test.Complex;

public record GetUserDetailRequest(int UserId, string[] Roles) : IRequest<UserDetailResponse>;
public record UserDetailResponse(string Name, List<string> Permissions);

public class ComplexUserHandler : IRequestHandler<GetUserDetailRequest, UserDetailResponse>
{
    [MediatorHttpGet(""GetUserDetail"", ""/users/{userId}/details"")]
    public Task<UserDetailResponse> Handle(GetUserDetailRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new UserDetailResponse(""Test"", new List<string>()));
}";

        var result = RunGeneratorAndGetFiles(source);
        return Verify(result);
    }

    static Dictionary<string, string> RunGeneratorAndGetFiles(string source)
    {
        var driver = BuildDriver(source);
        var runResult = driver.GetRunResult();
        var generatedFiles = new Dictionary<string, string>();
        
        foreach (var result in runResult.Results)
        {
            result.Exception.ShouldBeNull("Run result should not error");
            foreach (var generatedSource in result.GeneratedSources)
            {
                generatedFiles[generatedSource.HintName] = generatedSource.SourceText.ToString();
            }
        }
        
        return generatedFiles;
    }
    

    static GeneratorDriver BuildDriver(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Add minimal references for compilation
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MediatorHttpGroupAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, options);

        var generator = new MediatorEndpointSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }
}