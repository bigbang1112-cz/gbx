namespace BigBang1112.Gbx.Server.Models.Gbx;

public partial class Gbx
{
    public string? Class { get; set; }
    public CMwNod? Node { get; set; }

    public static void Validate(IList<GraphQLParser.AST.ASTNode> nodes, string className)
    {
        var operation = nodes.OfType<GraphQLParser.AST.GraphQLOperationDefinition>().FirstOrDefault();

        if (operation is null)
        {
            throw new Exception("Bad request: no operation");
        }

        if (nodes.Count > 1)
        {
            throw new Exception("Bad request: only one operation is allowed");
        }

        foreach (var node in operation.SelectionSet.Selections)
        {
            if (node is not GraphQLParser.AST.GraphQLField field)
            {
                continue;
            }

            switch (field.Name?.Value.ToString().ToLower())
            {
                case "class": continue;
                case "node":
                    ValidateClass(field.SelectionSet?.Selections, className);
                    continue;
                default:
                    throw new Exception($"Unknown field: {field.Name} ({field.Location.Start}-{field.Location.End})");
            }
        }
    }

    public static partial void ValidateClass(IList<GraphQLParser.AST.ASTNode>? nodes, string className);

    public partial Gbx Map(string className, GBX.NET.GameBox value);
}
