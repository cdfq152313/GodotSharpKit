using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension;

[Generator(LanguageNames.CSharp)]
public class HelloIncrementalGenerator : IIncrementalGenerator
{
    private Log Log;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Log = new Log(context, nameof(HelloIncrementalGenerator));
        context.RegisterPostInitializationOutput(
            ctx =>
                ctx.AddSource(
                    "HelloIncrementalGenerator.g.cs",
                    "public class HelloIncrementalGenerator{}"
                )
        );
        Init(context);
    }

    private void Init(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsSyntaxTarget,
            GetSyntaxTarget
        );
        var compilationProvider = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterSourceOutput(compilationProvider, (c, s) => OnExecute(s.Right, s.Left, c));
    }

    private bool IsSyntaxTarget(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (
            syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax
            || classDeclarationSyntax.AttributeLists.Count == 0
        )
        {
            return false;
        }

        var query =
            from attributeList in classDeclarationSyntax.AttributeLists
            from attribute in attributeList.Attributes
            select attribute;
        foreach (var attribute in query)
        {
            if (attribute.ToFullString() == nameof(OnReadyClass))
            {
                return true;
            }
        }
        return false;
    }

    private ClassDeclarationSyntax GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        return (ClassDeclarationSyntax)context.Node;
    }

    private void OnExecute(
        ImmutableArray<ClassDeclarationSyntax> nodes,
        Compilation compilation,
        SourceProductionContext context
    )
    {
        try
        {
            foreach (var node in nodes.Distinct())
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                // var model = compilation.GetSemanticModel(node.SyntaxTree);
                // var symbol = model.GetDeclaredSymbol(Node(node));
                // var attribute = symbol.GetAttributes().SingleOrDefault(x => x.AttributeClass.Name == attributeType);
                // if (attribute is null) continue;
                //
                // var (generatedCode, error) = _GenerateCode(compilation, node, symbol, attribute);
                //

                context.AddSource(
                    $"{node.Identifier}.g.cs",
                    $"public partial class {node.Identifier} {{ public void HelloWorld(){{ }} }}"
                );
            }
        }
        catch (Exception e)
        {
            // Log.Debug("Exception!!");
            throw;
        }
    }
}
