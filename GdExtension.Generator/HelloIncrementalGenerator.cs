using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension;

[Generator(LanguageNames.CSharp)]
public class HelloIncrementalGenerator : IIncrementalGenerator
{
    private Log? Log;

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
        if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax)
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
            var attributeName = attribute.ToFullString();
            if (attributeName == nameof(OnReadyNode) || attributeName == nameof(OnReadyConnect))
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
                attribute.AttributeClass!.ContainingNamespace!.Name == typeof(OnReadyNode).Namespace
            let attrInfo = attribute.AttributeClass!.Name switch
            {
                nameof(OnReadyNode) when member is IFieldSymbol fieldSymbol
                    => new NodeInfo(
                        fieldSymbol.Name,
                        fieldSymbol.Type.Name,
                        fieldSymbol.Type.ContainingNamespace.Name
                    ),
                nameof(OnReadyConnect) when member is IMethodSymbol methodSymbol
                    => new ConnectInfo(),
                _ => new AttrInfo(),
            }
            group attrInfo by attrInfo.GetType();
        var dict = query.ToDictionary(v => v.Key, v => v.ToList());
        if (dict.Count == 0)
        {
            return null;
        }

        return new RootInfo(classSymbol.ContainingNamespace.Name, classSymbol.Name, dict);
    }

    private void OnExecute(ImmutableArray<RootInfo> array, SourceProductionContext context)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var prefix = info.Namespace == "" ? "" : $"{info.Namespace}_";
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var nodeInfos = info.AttrInfoDict.ContainsKey(typeof(NodeInfo))
                ? info.AttrInfoDict[typeof(NodeInfo)].OfType<NodeInfo>()
                : new List<NodeInfo>();
            var readyStatement = string.Join(
                "\n",
                nodeInfos.Select(v =>
                {
                    var prefix = v.FieldTypeNamespace == "" ? "" : $"{v.FieldTypeNamespace}.";
                    return $"{v.FieldName} = GetNode<{prefix}{v.FieldType}>(\"{v.FieldName}\");";
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

record RootInfo(string Namespace, string ClassName, IDictionary<Type, List<AttrInfo>> AttrInfoDict);

record AttrInfo;

record NodeInfo(string FieldName, string FieldType, string FieldTypeNamespace) : AttrInfo;

record ConnectInfo() : AttrInfo;
