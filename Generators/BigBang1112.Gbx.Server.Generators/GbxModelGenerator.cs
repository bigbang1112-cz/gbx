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

        var builder = new StringBuilder("namespace BigBang1112.Gbx.Server.Models.Gbx;\n");
        
        foreach (var typeSymbol in enginesNamespace.GetNamespaceMembers().SelectMany(x => x.GetTypeMembers()))
        {
            if (!classSelection.IsEmpty && !classSelection.Contains(typeSymbol.Name))
            {
                continue;
            }
            
            WriteClass(builder, typeSymbol, indent: 0, classSelection);
        }

        context.AddSource("GeneratedGbxModels.cs", builder.ToString());
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

    private static bool IncludesNodeTypeSymbol(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                if (IsNodeTypeSymbol(typeArgument))
                {
                    return true;
                }
            }
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            return IsNodeTypeSymbol(arrayTypeSymbol.ElementType);
        }

        return IsNodeTypeSymbol(typeSymbol);
    }

    private static void WriteClass(StringBuilder builder, ITypeSymbol typeSymbol, int indent, ImmutableHashSet<string> classSelection)
    {
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public || typeSymbol.TypeKind == TypeKind.Interface)
        {
            return;
        }

        builder.AppendLine();
        builder.Append(indent, "public class ");
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

            if (nodeMemberAtt is null || (!classSelection.Contains(propertySymbol.Type.Name) && IncludesNodeTypeSymbol(propertySymbol.Type)))
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
            
            WriteClass(builder, innerTypeSymbol, indent + 1, classSelection);
        }
        
        builder.AppendLine(indent, "}");
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
                AppendType(builder, namedTypeSymbol.TypeArguments[0]);
                return;
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
