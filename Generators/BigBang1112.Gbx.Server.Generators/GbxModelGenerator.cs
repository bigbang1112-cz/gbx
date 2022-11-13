using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace BigBang1112.Gbx.Server.Generators;

[Generator]
public class GbxModelGenerator : ISourceGenerator
{
    private static bool Debug = false;
    
    public void Initialize(GeneratorInitializationContext context)
    {
        if (Debug && !Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDir))
        {
            throw new Exception("build_property.projectdir not found");
        }

        var classSelection = ImmutableHashSet<string>.Empty;

        var classSelectionFile = Path.Combine(projectDir, "Gbx.txt");

        if (File.Exists(classSelectionFile))
        {
            classSelection = File.ReadAllLines(classSelectionFile)
                .Select(x => x.Trim())
                .ToImmutableHashSet();
        }

        var gbxnet = context.Compilation.SourceModule.ReferencedAssemblySymbols.First(x => x.Name == "GBX.NET");
        var enginesNamespace = gbxnet.GlobalNamespace
            .GetNamespaceMembers().First(x => x.Name == "GBX")
            .GetNamespaceMembers().First(x => x.Name == "NET")
            .GetNamespaceMembers().First(x => x.Name == "Engines");

        var engineClasses = GetAllEngineClasses(enginesNamespace).ToImmutableList();

        foreach (var typeSymbol in engineClasses)
        {
            if (!classSelection.IsEmpty && !classSelection.Contains(typeSymbol.Name))
            {
                continue;
            }

            var builder = new StringBuilder("namespace BigBang1112.Gbx.Server.Models.Gbx;\n");
            WriteClass(builder, typeSymbol, indent: 0, classSelection, engineClasses);
            context.AddSource($"{typeSymbol.Name}.g.cs", builder.ToString());
        }

        var gbxBuilder = new StringBuilder("namespace BigBang1112.Gbx.Server.Models.Gbx;\n");
        WriteGbxClass(gbxBuilder, classSelection, engineClasses);
        context.AddSource("Gbx.g.cs", gbxBuilder.ToString());
    }

    private static IEnumerable<INamedTypeSymbol> GetAllEngineClasses(INamespaceSymbol enginesNamespace)
    {
        return enginesNamespace.GetNamespaceMembers().SelectMany(x => x.GetTypeMembers());
    }

    private void WriteGbxClass(StringBuilder builder, ImmutableHashSet<string> classSelection, IEnumerable<INamedTypeSymbol> engineTypeSymbols)
    {
        builder.AppendLine();
        builder.AppendLine("public partial class Gbx");
        builder.AppendLine("{");
        builder.AppendLine("    public static partial void ValidateClass(IList<GraphQLParser.AST.ASTNode> nodes, string className)");
        builder.AppendLine("    {");
        builder.AppendLine("        switch (className)");
        builder.AppendLine("        {");

        foreach (var typeSymbol in engineTypeSymbols)
        {
            if (!classSelection.Contains(typeSymbol.Name))
            {
                continue;
            }

            builder.Append("            case \"");
            builder.Append(typeSymbol.Name);
            builder.AppendLine("\":");
            builder.Append("                ");
            builder.Append(typeSymbol.Name);
            builder.AppendLine(".Validate(nodes);");
            builder.AppendLine("                break;");
        }

        builder.AppendLine("            default: throw new Exception($\"Unknown class: {className}\");");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        
        builder.AppendLine();
        builder.AppendLine("    public partial Gbx Map(string className, GBX.NET.GameBox value) => new()");
        builder.AppendLine("    {");
        builder.AppendLine("        Class = className,");
        builder.AppendLine("        Node = value.Node is null ? null : className switch");
        builder.AppendLine("        {");

        foreach (var typeSymbol in engineTypeSymbols)
        {
            if (!classSelection.Contains(typeSymbol.Name))
            {
                continue;
            }

            builder.AppendLine($"            \"{typeSymbol.Name}\" => {typeSymbol.Name}.Map((GBX.NET.Engines.{typeSymbol.ContainingNamespace.Name}.{typeSymbol.Name})value.Node),");
        }

        builder.AppendLine("            _ => throw new Exception($\"Unknown class: {className}\")");
        builder.AppendLine("        }");
        builder.AppendLine("    };");
        builder.AppendLine("}");
    }

    private static bool IsNodeTypeSymbol(ITypeSymbol typeSymbol)
    {
        var t = typeSymbol.BaseType;
        
        while (true)
        {
            if (t is null)
            {
                break;
            }

            if (t.Name == "CMwNod")
            {
                return true;
            }

            t = t.BaseType;
        }

        return false;
    }

    private static bool IncludesAllowedNodeTypeSymbolOrAnyOther(ITypeSymbol typeSymbol, ImmutableHashSet<string> classSelection)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                if (!IsNodeTypeSymbol(typeArgument))
                {
                    continue;
                }
                
                if (!classSelection.Contains(typeArgument.Name))
                {
                    return false;
                }

                return true;
            }
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol
            && IsNodeTypeSymbol(arrayTypeSymbol.ElementType)
            && !classSelection.Contains(arrayTypeSymbol.ElementType.Name))
        {
            return false;
        }

        if (IsNodeTypeSymbol(typeSymbol) && !classSelection.Contains(typeSymbol.Name))
        {
            return false;
        }
        
        return true;
    }

    private static void WriteClass(StringBuilder builder,
        ITypeSymbol typeSymbol,
        int indent,
        ImmutableHashSet<string> classSelection,
        ImmutableList<INamedTypeSymbol> engineClasses)
    {
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public || typeSymbol.TypeKind == TypeKind.Interface)
        {
            return;
        }

        builder.AppendLine();
        builder.Append(indent, "public ");

        if (typeSymbol.IsAbstract)
        {
            builder.Append("abstract ");
        }

        builder.Append("class ");
        builder.Append(typeSymbol.Name);

        if (typeSymbol.BaseType is not null && typeSymbol.BaseType is not { Name: "Object" or "Node" or "ValueType" })
        {
            builder.Append(" : ");
            AppendType(builder, typeSymbol.BaseType);
        }

        builder.AppendLine();
        builder.AppendLine(indent, "{");

        foreach (var propertySymbol in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var nodeMemberAtt = propertySymbol.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass?.Name == "NodeMemberAttribute");

            if (nodeMemberAtt is null || !IncludesAllowedNodeTypeSymbolOrAnyOther(propertySymbol.Type, classSelection))
            {
                continue;
            }

            builder.Append(indent + 1, "public ");
            AppendType(builder, propertySymbol.Type);
            builder.Append('?');

            builder.Append(' ');
            builder.Append(propertySymbol.Name);
            builder.AppendLine(" { get; set; }");
        }

        builder.AppendLine();
        builder.AppendLine(indent + 1, "public static void Validate(IList<GraphQLParser.AST.ASTNode>? nodes)");
        builder.AppendLine(indent + 1, "{");
        builder.AppendLine(indent + 2, "if (nodes is null) return;");
        builder.AppendLine();
        builder.AppendLine(indent + 2, "foreach (var node in nodes)");
        builder.AppendLine(indent + 2, "{");
        builder.AppendLine(indent + 3, "if (node is not GraphQLParser.AST.GraphQLField field)");
        builder.AppendLine(indent + 3, "{");
        builder.AppendLine(indent + 4, "continue;");
        builder.AppendLine(indent + 3, "}");
        builder.AppendLine();
        builder.AppendLine(indent + 3, "var fieldName = field.Name?.Value.ToString().ToLower();");
        builder.AppendLine();
        builder.AppendLine(indent + 3, "if (Validate(field, fieldName))");
        builder.AppendLine(indent + 3, "{");
        builder.AppendLine(indent + 4, "continue;");
        builder.AppendLine(indent + 3, "}");

        if (typeSymbol.BaseType is not null && IsNodeTypeSymbol(typeSymbol.BaseType))
        {
            builder.AppendLine();
            builder.Append(indent + 3, "if (");
            builder.Append(typeSymbol.BaseType.Name);
            builder.AppendLine(".Validate(field, fieldName))");
            builder.AppendLine(indent + 3, "{");
            builder.AppendLine(indent + 4, "continue;");
            builder.AppendLine(indent + 3, "}");
        }

        foreach (var potentialInheritingTypeSymbol in engineClasses)
        {
            if (!classSelection.Contains(potentialInheritingTypeSymbol.Name) || potentialInheritingTypeSymbol.BaseType?.Name != typeSymbol.Name)
            {
                continue;
            }

            builder.AppendLine();
            builder.Append(indent + 3, "if (");
            builder.Append(potentialInheritingTypeSymbol.Name);
            builder.AppendLine(".Validate(field, fieldName))");
            builder.AppendLine(indent + 3, "{");
            builder.AppendLine(indent + 4, "continue;");
            builder.AppendLine(indent + 3, "}");
        }

        builder.AppendLine();
        builder.AppendLine(indent + 3, "throw new Exception($\"Unknown field: {fieldName} ({field.Location.Start}-{field.Location.End})\");");
        builder.AppendLine(indent + 2, "}");
        builder.AppendLine(indent + 1, "}");

        builder.AppendLine();
        builder.AppendLine(indent + 1, "public static bool Validate(GraphQLParser.AST.GraphQLField field, string fieldName)");
        builder.AppendLine(indent + 1, "{");
        builder.AppendLine(indent + 2, "switch (fieldName)");
        builder.AppendLine(indent + 2, "{");

        var propCounter = 0;

        foreach (var grouping in typeSymbol.GetMembers()
            .OfType<IPropertySymbol>().GroupBy<IPropertySymbol, bool>(HasNodeTypeSymbol))
        {
            var isNodeTypeSymbol = grouping.Key;

            foreach (var propertySymbol in grouping)
            {
                var nodeMemberAtt = propertySymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "NodeMemberAttribute");

                if (nodeMemberAtt is null || !IncludesAllowedNodeTypeSymbolOrAnyOther(propertySymbol.Type, classSelection))
                {
                    continue;
                }

                propCounter++;

                builder.Append(indent + 3, "case \"");
                builder.Append(propertySymbol.Name.ToLower());
                builder.AppendLine("\":");

                if (!isNodeTypeSymbol)
                {
                    continue;
                }

                var type = propertySymbol.Type;

                if (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0)
                {
                    type = namedTypeSymbol.TypeArguments[0];
                }

                builder.Append(indent + 4, type.Name);
                builder.AppendLine(".Validate(field.SelectionSet?.Selections);");
                builder.AppendLine(indent + 4, "return true;");
            }

            if (!isNodeTypeSymbol && propCounter > 0)
            {
                builder.AppendLine(indent + 4, "return true;");
            }
        }

        builder.AppendLine(indent + 3, "default: return false;");
        builder.AppendLine(indent + 2, "}");

        builder.AppendLine(indent + 1, "}");
        builder.AppendLine();

        builder.Append(indent + 1, "public static ");
        builder.Append(typeSymbol.Name);
        builder.Append(" Map(GBX.NET.Engines.");
        builder.Append(typeSymbol.ContainingNamespace.Name);
        builder.Append('.');

        JustWriteWholeType(builder, typeSymbol);

        builder.AppendLine(" value)");
        builder.AppendLine(indent + 1, "{");
        builder.AppendLine(indent + 2, "return new();");
        builder.AppendLine(indent + 1, "}");

        foreach (var innerTypeSymbol in typeSymbol.GetTypeMembers())
        {
            if (innerTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "ChunkAttribute"))
            {
                continue;
            }

            if (innerTypeSymbol.TypeKind == TypeKind.Enum)
            {
                WriteEnum(builder, innerTypeSymbol, indent + 1);
                continue;
            }

            WriteClass(builder, innerTypeSymbol, indent + 1, classSelection, engineClasses);
        }

        builder.AppendLine(indent, "}");
    }

    private static bool HasNodeTypeSymbol(IPropertySymbol x)
    {
        return IsNodeTypeSymbol(x.Type)
            || (x.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0 && IsNodeTypeSymbol(namedTypeSymbol.TypeArguments[0]));
    }

    private static void JustWriteWholeType(StringBuilder builder, ITypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is not null)
        {
            JustWriteWholeType(builder, typeSymbol.ContainingType);
            builder.Append('.');
        }
        
        builder.Append(typeSymbol.Name);
    }

    private static void WriteEnum(StringBuilder builder, INamedTypeSymbol innerTypeSymbol, int indent)
    {
        builder.AppendLine();
        builder.Append(indent, "public enum ");
        builder.Append(innerTypeSymbol.Name);
        builder.AppendLine();
        builder.AppendLine(indent, "{");

        foreach (var memberSymbol in innerTypeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            builder.Append(indent + 1, memberSymbol.Name);
            builder.Append(" = ");
            builder.Append(memberSymbol.ConstantValue);
            builder.AppendLine(",");
        }
        
        builder.AppendLine(indent, "}");
    }

    private static void AppendType(StringBuilder builder, ITypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is not null)
        {
            AppendType(builder, typeSymbol.ContainingType);
            builder.Append('.');
        }
        
        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            AppendType(builder, arrayTypeSymbol.ElementType);
            builder.Append("[]");
            return;
        }

        if (typeSymbol is not INamedTypeSymbol {IsGenericType: true} namedTypeSymbol)
        {
            var newType = typeSymbol.Name switch
            {
                "TimeInt32" => "int",
                "TimeSingle" => "float",
                _ => typeSymbol.Name
            };
            
            builder.Append(newType);
            return;
        }

        switch (namedTypeSymbol.Name)
        {
            case "Nullable":
            case "ExternalNode":
                AppendType(builder, namedTypeSymbol.TypeArguments[0]);
                return;
        }

        builder.Append(typeSymbol.Name);
        builder.Append("<");

        for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
        {
            if (i != 0)
            {
                builder.Append(", ");
            }

            AppendType(builder, namedTypeSymbol.TypeArguments[i]);
        }

        builder.Append(">");
    }
}
