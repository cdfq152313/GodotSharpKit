using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GdExtension;

[Generator]
public class ResourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AnalyzerConfigOptionsProvider;
        var provider = context.AdditionalTextsProvider
            .Where(v => v.Path.EndsWith(".tscn"))
            .Combine(configProvider)
            .Select(Transform)
            .Collect();
        context.RegisterSourceOutput(provider, GenerateCode);
    }

    private ResourceInfo Transform(
        (AdditionalText additionalText, AnalyzerConfigOptionsProvider config) valueTuple,
        CancellationToken cancellationToken
    )
    {
        var (additionalText, config) = valueTuple;
        config.GlobalOptions.TryGetValue("build_property.projectdir", out var root);
        config
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Container", out var container);
        var originPath = additionalText.Path;
        var content = additionalText.GetText(cancellationToken)?.ToString() ?? "";
        var gdPath = $"res://{Path.GetRelativePath(root!, originPath).Replace("\\", "/")}";
        var name = Path.GetFileNameWithoutExtension(originPath);
        return new ResourceInfo(name, gdPath, container ?? "", content);
    }

    private void GenerateCode(SourceProductionContext context, ImmutableArray<ResourceInfo> array)
    {
        var indent = "    ";
        var dict = new Dictionary<string, StringBuilder>();
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!dict.ContainsKey(info.Container))
            {
                dict[info.Container] = new StringBuilder();
                if (info.Container != "")
                {
                    dict[info.Container].AppendLine(
                        $"{indent}public static class {info.Container}\n{indent}{{"
                    );
                }
            }

            if (info.Container != "")
            {
                dict[info.Container].Append(indent);
            }
            dict[info.Container].AppendLine(
                $"{indent}public static string {info.Name} = \"{info.GdPath}\";"
            );
        }

        foreach (var entry in dict)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (entry.Key != "")
            {
                entry.Value.AppendLine($"{indent}}}");
            }
        }

        context.AddSource(
            "AutoRes.g.cs",
            @$"
public static class AutoRes
{{
{string.Join("", dict.Select(v => v.Value))}
}}
"
        );
    }
}

record ResourceInfo(string Name, string GdPath, string Container, string Content);
