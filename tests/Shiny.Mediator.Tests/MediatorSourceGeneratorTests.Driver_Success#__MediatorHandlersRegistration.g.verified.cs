//HintName: __MediatorHandlersRegistration.g.cs
using Shiny.Mediator;

namespace TestAssembly;

[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Shiny.Mediator", "4.0.0")]
public static class __ShinyMediatorSourceGenExtensions {
	public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddDiscoveredMediatorHandlersFromTestAssembly(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
	{
		services.AddSingletonAsImplementedInterfaces<MyTests.SourceGenCommandHandler>();
		return services;
	}
}
