using GBX.NET;
using GBX.NET.Attributes;
using System.Reflection;

namespace BigBang1112.Gbx.Client.Models.UtilImport;

public class UtilImportTypeParam
{
    public required string TypeName { get; init; }
    public bool IsNodeType { get; init; }
    public bool Multiple { get; init; }
    public string[] Extensions { get; init; } = Array.Empty<string>();

    public GbxModel[] ImportedFiles { get; set; } = Array.Empty<GbxModel>();

    public static UtilImportTypeParam FromParameter(ParameterInfo param)
    {
        var pType = param.ParameterType;
        var multiple = false;

        if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            pType = pType.GetGenericArguments()[0];
            multiple = true;
        }

        var isNodeType = pType.IsSubclassOf(typeof(Node));
        var extensions = Array.Empty<string>();

        if (isNodeType)
        {
            extensions = pType.GetCustomAttributes()
                .OfType<NodeExtensionAttribute>()
                .Select(x => x.Extension)
                .Distinct()
                .ToArray();
        }

        return new UtilImportTypeParam()
        {
            TypeName = pType.Name,
            IsNodeType = isNodeType,
            Multiple = multiple,
            Extensions = extensions
        };
    }
}
