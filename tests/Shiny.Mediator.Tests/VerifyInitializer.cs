using System.Runtime.CompilerServices;

namespace Shiny.Mediator.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}