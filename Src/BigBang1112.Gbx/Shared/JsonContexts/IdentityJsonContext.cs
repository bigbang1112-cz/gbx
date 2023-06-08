using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Shared.JsonContexts;

[JsonSerializable(typeof(Identity))]
public partial class IdentityJsonContext : JsonSerializerContext
{
}
