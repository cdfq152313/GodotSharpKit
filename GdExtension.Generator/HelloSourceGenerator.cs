using Microsoft.CodeAnalysis;

namespace GdExtension
{
    [Generator]
    public class HelloSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
        }
    }
}