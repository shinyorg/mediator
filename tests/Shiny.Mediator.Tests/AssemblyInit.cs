using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Mediator.Tests;


/// <summary>
/// Registers the reflection-based <see cref="DefaultJsonTypeInfoResolver"/> on
/// <see cref="Shiny.Json.Default"/> once for the whole test assembly. Production code is expected
/// to register types explicitly via <c>[ShinyJsonContext]</c> / <c>[JsonSerializable]</c> for AOT
/// safety, but the tests serialize many ad-hoc and source-generated types whose contexts aren't
/// always registered — the reflection fallback gives them the same behavior the old
/// <c>SysTextJsonSerializerService</c> provided by default.
/// </summary>
internal static class AssemblyInit
{
    [ModuleInitializer]
    internal static void Init()
        => Shiny.Json.AddResolver(new DefaultJsonTypeInfoResolver());
}
