using System.Text.RegularExpressions;

namespace BigBang1112.Gbx.Client;

internal partial class RegexUtils
{
    [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])")]
    private static partial Regex RegexPascalCaseToKebabCase();

    internal static string PascalCaseToKebabCase(string str)
    {
        return RegexPascalCaseToKebabCase().Replace(str, "-$1").Trim().ToLower();
    }
}
