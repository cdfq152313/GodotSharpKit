using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GdExtension;

[Generator(LanguageNames.CSharp)]
public class ResourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AnalyzerConfigOptionsProvider;
        var resProvider = context.AdditionalTextsProvider
            .Combine(configProvider)
            .Select(ResTransform)
            .Collect();
        context.RegisterSourceOutput(resProvider, GenerateCode);
    }

    private ResourceInfo ResTransform(
        (AdditionalText additionalText, AnalyzerConfigOptionsProvider config) valueTuple,
        CancellationToken cancellationToken
    )
    {
        var (additionalText, optionsProvider) = valueTuple;
        optionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var root);
        optionsProvider.GlobalOptions.TryGetValue(
            "build_property.RootNamespace",
            out var rootNamespace
        );
        optionsProvider
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Container", out var container);
        optionsProvider
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Class", out var resourceType);
        var originPath = additionalText.Path;
        var content = additionalText.GetText(cancellationToken)?.ToString() ?? "";
        var relativePath = Path.GetRelativePath(root!, originPath).Replace("\\", "/");
        var gdPath = $"res://{relativePath}";
        var csPath = Path.ChangeExtension(gdPath, ".cs");
        var name = Path.GetFileNameWithoutExtension(originPath);

        string SceneType()
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                list.Add(rootNamespace);
            }

            list.AddRange(relativePath.Replace(".tscn", "").Split("/"));
            return string.Join(".", list);
        }
        var type = resourceType switch
        {
            "PackedScene" when content.Contains(csPath) => $"SceneRes<{SceneType()}>",
            "PackedScene" => "SceneRes<Node>",
            _ => $"Res<{(string.IsNullOrEmpty(resourceType) ? "Resource" : resourceType)}>",
        };
        return new ResourceInfo(
            name,
            gdPath!,
            string.IsNullOrEmpty(container) ? "AutoRes" : container,
            type
        );
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

            dict[info.Container].AppendLine(
                $"{indent}public static {info.Type} {info.Name} = new(\"{info.GdPath}\");"
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

record ResourceInfo(string Name, string GdPath, string Container, string Type);
