using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Shared.JsonContexts;

[JsonSerializable(typeof(string[]))]
public partial class StringArrayJsonContext : JsonSerializerContext
{
}
