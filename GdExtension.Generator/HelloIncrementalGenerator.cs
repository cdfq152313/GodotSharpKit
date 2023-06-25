using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        var syntaxProvider = context.SyntaxProvider
            .CreateSyntaxProvider(IsSyntaxTarget, GetSyntaxTarget)
            .Where(v => v is not null)
            .Select((s, _) => s!);
        context.RegisterSourceOutput(syntaxProvider.Collect(), (c, s) => OnExecute(s, c));
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
            from member in classDeclarationSyntax.Members
            from attributeList in member.AttributeLists
            from attribute in attributeList.Attributes
            select attribute;

        foreach (var attribute in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (attribute.ToFullString() == nameof(OnReadyNode))
            {
                return true;
            }
        }
        return false;
    }

    private RootInfo? GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var model = context.SemanticModel;
        var classSymbol = (ITypeSymbol)model.GetDeclaredSymbol(context.Node)!;
        var query =
            from member in classSymbol.GetMembers()
            from attribute in member.GetAttributes()
            where
                attribute.AttributeClass.ContainingNamespace.Name == typeof(OnReadyNode).Namespace
                && attribute.AttributeClass.Name == nameof(OnReadyNode)
            select member;
        var list = new List<GetNodeInfo>();
        foreach (var member in query.OfType<IFieldSymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(
                new GetNodeInfo(
                    member.Name,
                    member.Type.Name,
                    member.Type.ContainingNamespace.ToDisplayString()
                )
            );
        }

        if (list.Count == 0)
        {
            return null;
        }
        return new RootInfo(classSymbol.ContainingNamespace.Name, classSymbol.Name, list);
    }

    private void OnExecute(ImmutableArray<RootInfo> array, SourceProductionContext context)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var prefix = info.Namespace == "" ? "" : $"{info.Namespace}_";
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var readyStatement = string.Join(
                "\n",
                info.NodeInfos.Select(v =>
                {
                    var prefix = v.Namespace == "" ? "" : $"{v.Namespace}.";
                    return $"{v.Name} = GetNode<{prefix}{v.Type}>(\"{v.Name}\");";
                })
            );
            context.AddSource(
                $"{prefix}{info.ClassName}.g.cs",
                @$"
{namespaceStatement}
public partial class {info.ClassName} 
{{ 
    public void OnReady()
    {{
        {readyStatement}
    }} 
}}
"
            );
        }
    }
}

record RootInfo(string Namespace, string ClassName, List<GetNodeInfo> NodeInfos);

record GetNodeInfo(string Name, string Type, string Namespace);
