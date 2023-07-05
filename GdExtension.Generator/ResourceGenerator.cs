using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GdExtension;

[Generator]
public class ResourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AnalyzerConfigOptionsProvider;
        var tscnProvider = context.AdditionalTextsProvider
            .Where(v => v.Path.EndsWith(".tscn"))
            .Combine(configProvider)
            .Select(ResTransform)
            .Collect();
        context.RegisterSourceOutput(tscnProvider, GenerateCode);
    }

    private ResourceInfo ResTransform(
        (AdditionalText additionalText, AnalyzerConfigOptionsProvider config) valueTuple,
        CancellationToken cancellationToken
    )
    {
        var (additionalText, optionsProvider) = valueTuple;
        optionsProvider
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Container", out var container);
        var originPath = additionalText.Path;
        var content = additionalText.GetText(cancellationToken)?.ToString() ?? "";
        var gdPath = ToGdPath(optionsProvider, originPath);
        var name = Path.GetFileNameWithoutExtension(originPath);
        return new ResourceInfo(
            name,
            gdPath,
            string.IsNullOrEmpty(container) ? "AutoRes" : container,
            content
        );
    }

    private string ToGdPath(AnalyzerConfigOptionsProvider optionsProvider, string originPath)
    {
        optionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var root);
        return $"res://{Path.GetRelativePath(root!, originPath).Replace("\\", "/")}";
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
                dict[info.Container] = new StringBuilder(
                    $"public static class {info.Container}\n{{\n"
                );
            }

            var type = "Res<Resource>";
            dict[info.Container].AppendLine(
                $"{indent}public static {type} {info.Name} = new(\"{info.GdPath}\");"
            );
        }

        foreach (var entry in dict)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            entry.Value.AppendLine("}");
        }

        context.AddSource(
            "AutoRes.g.cs",
            @$"
using GdExtension;
using Godot;

{string.Join("", dict.Select(v => v.Value))}
"
        );
    }
}

record ResourceInfo(string Name, string GdPath, string Container, string Content);

record CsInfo(string GdPath, string Namespace);
