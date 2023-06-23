using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension;

[Generator(LanguageNames.CSharp)]
public class HelloIncrementalGenerator : IIncrementalGenerator
{
    private Log Log = new Log(nameof(HelloIncrementalGenerator));

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            ctx =>
                ctx.AddSource(
                    "HelloIncrementalGenerator.g.cs",
                    "public class HelloIncrementalGenerator{}\n"
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
            if (attribute.Name.ToString() == typeof(OnReadyClass).FullName)
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
        Log.Debug("Hello");
        Log.Flush(context);
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
                    $"{node.Identifier.Text}.g.cs",
                    "public partial class LaunchScreen { public void HelloWorld(){} }"
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
