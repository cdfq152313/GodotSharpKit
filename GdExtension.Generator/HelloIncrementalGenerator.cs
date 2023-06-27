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

    private ClassInfo? GetSyntaxTarget(
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
            select new { member, attribute };
        var dict = new Dictionary<Type, List<ActionInfo>>();
        foreach (var data in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actionInfo = data.attribute.AttributeClass!.Name switch
            {
                nameof(OnReadyNode) when data.member is IFieldSymbol fieldSymbol
                    => new GetInfo(
                        fieldSymbol.Name,
                        fieldSymbol.Type.Name,
                        fieldSymbol.Type.ContainingNamespace.Name
                    ),
                nameof(OnReadyConnect) when data.member is IMethodSymbol methodSymbol
                    => new ConnectInfo(
                        methodSymbol.Name,
                        (string)data.attribute.ConstructorArguments[0].Value!,
                        (string)data.attribute.ConstructorArguments[1].Value!
                    ),
                _ => new ActionInfo(),
            };
            if (!dict.ContainsKey(actionInfo.GetType()))
            {
                dict[actionInfo.GetType()] = new List<ActionInfo>();
            }
            dict[actionInfo.GetType()].Add(actionInfo);
        }
        if (dict.Count == 0)
        {
            return null;
        }
        return new ClassInfo(classSymbol.ContainingNamespace.Name, classSymbol.Name, dict);
    }

    private void OnExecute(ImmutableArray<ClassInfo> array, SourceProductionContext context)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var getInfos = info.ActionInfoDict
                .GetValue(typeof(GetInfo), new List<ActionInfo>())!
                .OfType<GetInfo>();
            var getNodeStatement = string.Join(
                "\n        ",
                getInfos.Select(
                    v =>
                        $"{v.FieldName} = GetNode<{(v.FieldTypeNamespace == "" ? "" : $"{v.FieldTypeNamespace}.")}{v.FieldType}>(\"{v.GodotName}\");"
                )
            );
            var connectInfos = info.ActionInfoDict
                .GetValue(typeof(ConnectInfo), new List<ActionInfo>())!
                .OfType<ConnectInfo>();
            var connectSignalStatement = string.Join(
                "\n        ",
                connectInfos.Select(
                    v =>
                        $"{(v.Source.Length == 0 ? "" : $"{v.Source}.")}{v.Signal} += {v.MethodName};"
                )
            );
            context.AddSource(
                $"{(info.Namespace == "" ? "" : $"{info.Namespace.Replace(".", "_")}_")}{info.ClassName}.g.cs",
                @$"
{namespaceStatement}
public partial class {info.ClassName} 
{{ 
    public void OnReady()
    {{
        {getNodeStatement}
        {connectSignalStatement}
    }} 
}}
"
            );
        }
    }
}

record ClassInfo(
    string Namespace,
    string ClassName,
    IDictionary<Type, List<ActionInfo>> ActionInfoDict
);

record ActionInfo;

record GetInfo(string FieldName, string FieldType, string FieldTypeNamespace) : ActionInfo
{
    public string GodotName =>
        FieldName.StartsWith("_")
            ? FieldName[1].ToString().ToUpper() + FieldName.Substring(2)
            : FieldName;
}

record ConnectInfo(string MethodName, string Source, string Signal) : ActionInfo;
