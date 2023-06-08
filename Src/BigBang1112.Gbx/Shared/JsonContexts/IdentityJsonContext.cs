using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Shared.JsonContexts;

[JsonSerializable(typeof(Identity))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class IdentityJsonContext : JsonSerializerContext
{
}
