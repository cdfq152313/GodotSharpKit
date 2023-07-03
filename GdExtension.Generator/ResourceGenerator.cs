using Microsoft.CodeAnalysis;

namespace GdExtension;

[Generator]
public class ResourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AdditionalTextsProvider
            .Where(v => v.Path.EndsWith("gdExtension.config.json"))
            .Select((v, c) => v.GetText(c)?.ToString())
            .Where(v => v is not null)
            .Select((v, c) => v!)
            .Select(GetConfig);
        context.RegisterSourceOutput(configProvider, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, Config arg2)
    {
        context.AddSource("ResourceGenerator.g.cs", "public class HelloResourceGenerator {}");
    }

    private Config GetConfig(string arg1, CancellationToken arg2)
    {
        return ConfigUtil.Read(arg1);
    }
}
