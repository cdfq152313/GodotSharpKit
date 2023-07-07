using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GodotSharpKit.Generator;

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
        var rootNamespace = configProvider.Select(
            (v, c) =>
            {
                v.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return string.IsNullOrEmpty(rootNamespace) ? "" : $"\nnamespace {rootNamespace};";
            }
        );
        context.RegisterSourceOutput(resProvider.Combine(rootNamespace), GenerateCode);
    }

    private ResourceInfo ResTransform(
        (AdditionalText additionalText, AnalyzerConfigOptionsProvider config) valueTuple,
        CancellationToken cancellationToken
    )
    {
        var (additionalText, optionsProvider) = valueTuple;
        optionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var root);
        optionsProvider
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Container", out var container);
        optionsProvider
            .GetOptions(additionalText)
            .TryGetValue("build_metadata.AdditionalFiles.Type", out var resourceType);
        var originPath = additionalText.Path;
        var content = additionalText.GetText(cancellationToken)?.ToString() ?? "";
        var relativePath = originPath.Replace(root!, "").Replace("\\", "/");
        var gdPath = $"res://{relativePath}";
        var csPath = Path.ChangeExtension(gdPath, ".cs");
        var name = Path.GetFileNameWithoutExtension(originPath);

        var type = resourceType switch
        {
            "PackedScene" when content.Contains(csPath)
                => $"SceneRes<{string.Join(".", relativePath.Replace(".tscn", "").Split('/'))}>",
            "PackedScene" => "SceneRes<Node>",
            _ => $"Res<{(string.IsNullOrEmpty(resourceType) ? "Resource" : resourceType)}>",
        };
        return new ResourceInfo(
            name,
            gdPath!,
            string.IsNullOrEmpty(container) ? "AutoRes" : container!,
            type
        );
    }

    private void GenerateCode(
        SourceProductionContext context,
        (ImmutableArray<ResourceInfo> Left, string Right) valueTuple
    )
    {
        var (resourceInfos, namespaceStatement) = valueTuple;
        var indent = "    ";
        var dict = new Dictionary<string, StringBuilder>();
        foreach (var info in resourceInfos)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!dict.ContainsKey(info.Container))
            {
                dict[info.Container] = new StringBuilder(
                    $"public static class {info.Container}\n{{\n"
                );
            }

            dict[info.Container].AppendLine(
                $"{indent}public static readonly {info.Type} {info.Name} = new(\"{info.GdPath}\");"
            );
        }

        foreach (var entry in dict)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            entry.Value.AppendLine("}");
        }

        context.AddSource(
            "AutoRes.g.cs",
            @$"using GodotSharpKit;
using Godot;
{namespaceStatement}

{string.Join("", dict.Select(v => v.Value))}
"
        );
    }

    record ResourceInfo(string Name, string GdPath, string Container, string Type);
}
