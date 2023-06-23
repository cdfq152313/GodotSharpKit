using Microsoft.CodeAnalysis;
using GeneratorContext = Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext;

namespace GdExtension;

[Generator]
public class HelloGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("HelloGenerator.g.cs", "public class HelloGenerator {}");
    }
}
