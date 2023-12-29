using System.Text;
using Microsoft.CodeAnalysis;

namespace GodotSharpKit;

public static class Util
{
    public static string FullName(this ISymbol namespaceSymbol)
    {
        var list = new List<string>();
        while (!string.IsNullOrEmpty(namespaceSymbol.Name))
        {
            if (namespaceSymbol is INamedTypeSymbol { IsGenericType: true } nType)
            {
                var generic = string.Join(',', nType.TypeArguments.Select(v => v.FullName()));
                list.Add($"{namespaceSymbol.Name}<{generic}>");
            }
            else
            {
                list.Add(namespaceSymbol.Name);
            }
            namespaceSymbol = namespaceSymbol.ContainingSymbol;
        }

        list.Reverse();
        return string.Join('.', list);
    }

    public static string ConcatDot(this string a, string b)
    {
        return a.Length > 0 ? $"{a}.{b}" : b;
    }

    public static void AppendIndent(this StringBuilder sb, int times = 1)
    {
        for (var i = 0; i < times; ++i)
        {
            sb.Append("    ");
        }
    }
}
