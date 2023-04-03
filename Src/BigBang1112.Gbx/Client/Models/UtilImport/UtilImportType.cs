using System.ComponentModel;
using System.Reflection;

namespace BigBang1112.Gbx.Client.Models.UtilImport;

public class UtilImportType
{
    public required List<UtilImportTypeParam> Parameters { get; init; }
    public string? Description { get; init; }

    public bool HasImportedFile { get; set; }

    public static UtilImportType FromConstructor(ConstructorInfo ctor)
    {
        var parameters = ctor.GetParameters();

        return new UtilImportType()
        {
            Parameters = parameters.Select(UtilImportTypeParam.FromParameter).ToList(),
            Description = ctor.GetCustomAttribute<DescriptionAttribute>()?.Description,
        };
    }
}
