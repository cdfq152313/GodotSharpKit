using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension.Generator;

[Generator(LanguageNames.CSharp)]
public class OnReadyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(GdExtNode).FullName!,
            IsSyntaxTarget,
            GetSyntaxTarget
        );
        context.RegisterSourceOutput(syntaxProvider.Collect(), OnExecute);
    }

    private bool IsSyntaxTarget(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }

    private ClassInfo GetSyntaxTarget(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var classSymbol = (ITypeSymbol)context.TargetSymbol;
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
                        data.attribute.ConstructorArguments[0].Value as string,
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
        return new ClassInfo(classSymbol.ContainingNamespace.Name, classSymbol.Name, dict);
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<ClassInfo> array)
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
                        $"{v.FieldName} = GetNode<{(v.FieldTypeNamespace == "" ? "" : $"{v.FieldTypeNamespace}.")}{v.FieldType}>(\"{v.FieldPath}\");"
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

record GetInfo(string FieldName, string FieldType, string? _fieldPath, string FieldTypeNamespace)
    : ActionInfo
{
    public string FieldPath => _fieldPath ?? _uniqueName;
    private string _uniqueName =>
        FieldName.StartsWith("_")
            ? $"%{FieldName[1].ToString().ToUpper()}{FieldName.Substring(2)}"
            : $"%{FieldName}";
}

record ConnectInfo(string MethodName, string Source, string Signal) : ActionInfo;
