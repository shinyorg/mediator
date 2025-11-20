namespace Shiny.Mediator.SourceGenerators;

public static class Constants
{
    public const string GeneratedCodeAttributeString = "[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Shiny.Mediator.SourceGenerators\", \"6.0.0\")]";
    
    // registry defaults
    public const bool DefaultRegistryUseInternal = false;
    public const string DefaultRegistryRegistrationMethodName = "AddMediatorRegistry";
    public const string DefaultRequestExecutorClassName = "GeneratedMediatorRequestExecutor";
    public const string DefaultStreamRequestExecutorClassName = "GeneratedMediatorStreamRequestExecutor";
    
    // user httpclient
    public const bool DefaultHttpRegistrationUseInternal = false;
    public const string DefaultHttpRegistrationClassName = "__ShinyMediatorStrongTypeHttpClient";
    public const string DefaultHttpRegistrationMethodName = "AddStrongTypedHttpClient";
    
    // openapi httpclient
    public const bool DefaultOpenApiRegistrationUseInternal = false;
    public const bool DefaultOpenApiGenerateJsonConverters = false;
    public const string DefaultOpenApiRegistrationClassName = "__ShinyMediatorOpenApiClient";
    public const string DefaultOpenApiRegistrationMethodName = "AddGeneratedOpenApiClient";
}