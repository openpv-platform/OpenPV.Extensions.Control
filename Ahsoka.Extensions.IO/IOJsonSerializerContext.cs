using Ahsoka.Core.Utility;
using Ahsoka.Services.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Ahsoka.Core;

#pragma warning disable CA2255 

/// <summary>
/// System Class for Handling Source Generated Json Files
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    IncludeFields = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(IOApplicationConfiguration))]
public partial class IOJsonSerializerContext : JsonSerializerContext
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        JsonUtility.AddTypeResolver(Default);
    }
}