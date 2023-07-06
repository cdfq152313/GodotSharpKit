using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdExtension.Generator;

[Generator(LanguageNames.CSharp)]
public class OnReadyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                typeof(GdExtNode).FullName!,
                IsSyntaxTarget,
                GetSyntaxTarget
            )
            .WithComparer(new RootEqual());
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
        var actionList = new List<ActionInfo>();
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
                nameof(OnReady) when data.member is IMethodSymbol methodSymbol
                    => new MethodInfo(
                        methodSymbol.Name,
                        (int)data.attribute.ConstructorArguments[0].Value!
                    ),
                nameof(OnReadyLast) when data.member is IMethodSymbol methodSymbol
                    => new MethodInfoLast(methodSymbol.Name),
                _ => new ActionInfo(),
            };
            actionList.Add(actionInfo);
        }
        return new ClassInfo(classSymbol.ContainingNamespace.Name, classSymbol.Name, actionList);
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<ClassInfo> array)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var getNodeStatement = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<GetInfo>()
                    .Select(
                        v =>
                            $"{v.FieldName} = GetNode<{(v.FieldTypeNamespace == "" ? "" : $"{v.FieldTypeNamespace}.")}{v.FieldType}>(\"{v.FieldPath}\");"
                    )
            );
            var connectSignalStatement = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<ConnectInfo>()
                    .Select(
                        v =>
                            $"{(v.Source.Length == 0 ? "" : $"{v.Source}.")}{v.Signal} += {v.MethodName};"
                    )
            );

            var methodInfo = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<MethodInfo>()
                    .OrderBy(v => v.Order)
                    .Select(v => $"{v.MethodName}();")
            );
            var methodInfoLast = string.Join(
                "\n        ",
                info.ActionList.OfType<MethodInfoLast>().Select(v => $"{v.MethodName}();")
            );

            context.AddSource(
                $"{(info.Namespace == "" ? "" : $"{info.Namespace.Replace(".", "_")}_")}{info.ClassName}.g.cs",
                @$"{namespaceStatement}

public partial class {info.ClassName} 
{{ 
    public override void _Ready()
    {{
        base._Ready();
        {getNodeStatement}
        {connectSignalStatement}
        {methodInfo}
        {methodInfoLast}
    }} 
}}
"
            );
        }
    }

    record ClassInfo(string Namespace, string ClassName, List<ActionInfo> ActionList);

    record ActionInfo;

    record GetInfo(
        string FieldName,
        string FieldType,
        string? _fieldPath,
        string FieldTypeNamespace
    ) : ActionInfo
    {
        public string FieldPath => _fieldPath ?? _uniqueName;
        private string _uniqueName =>
            FieldName.StartsWith("_")
                ? $"%{FieldName[1].ToString().ToUpper()}{FieldName.Substring(2)}"
                : $"%{FieldName}";
    }

    record ConnectInfo(string MethodName, string Source, string Signal) : ActionInfo;

    record MethodInfo(string MethodName, int Order) : ActionInfo;

    record MethodInfoLast(string MethodName) : ActionInfo;

    class RootEqual : IEqualityComparer<ClassInfo>
    {
        public bool Equals(ClassInfo? x, ClassInfo? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return x.Namespace == y.Namespace
                && x.ClassName == y.ClassName
                && x.ActionList.SequenceEqual(y.ActionList);
        }

        public int GetHashCode(ClassInfo obj)
        {
            return HashCode.Combine(
                obj.Namespace,
                obj.ClassName,
                obj.ActionList.GetSequenceHashCode()
            );
        }
    }
}
