using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension
{
    public class ObjectSyntaxReceiver : ISyntaxContextReceiver
    {
        public ClassDeclarationSyntax? MyClassSyntax { get; private set; }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (
                context.Node is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.Identifier.Value is string identifierValue
                && identifierValue.Contains("LaunchScreen")
            )
            {
                MyClassSyntax = classDeclarationSyntax;
            }
        }
    }
}
