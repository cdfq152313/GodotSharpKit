using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotSharpKit.Generator;

[Generator(LanguageNames.CSharp)]
public class OnReadyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                typeof(OnReady).FullName!,
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

    private Root GetSyntaxTarget(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var classSymbol = (ITypeSymbol)context.TargetSymbol;
        var query =
            from member in classSymbol.GetMembers()
            from attribute in member.GetAttributes()
            where
                attribute.AttributeClass!.ContainingNamespace!.Name == typeof(OnReadyGet).Namespace
            select new { member, attribute };
        var actionList = new List<OnReadyAction>();
        foreach (var data in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actionInfo = data.attribute.AttributeClass!.Name switch
            {
                nameof(OnReadyGet) when data.member is IFieldSymbol fieldSymbol
                    => new Get(
                        fieldSymbol.Name,
                        fieldSymbol.Type.Name,
                        data.attribute.ConstructorArguments[0].Value as string,
                        fieldSymbol.Type.ContainingNamespace.Name
                    ),
                nameof(OnReadyConnect) when data.member is IMethodSymbol methodSymbol
                    => new Connect(
                        methodSymbol.Name,
                        (string)data.attribute.ConstructorArguments[0].Value!,
                        (string)data.attribute.ConstructorArguments[1].Value!
                    ),
                nameof(OnReadyRun) when data.member is IMethodSymbol methodSymbol
                    => new Run(
                        methodSymbol.Name,
                        (int)data.attribute.ConstructorArguments[0].Value!
                    ),
                nameof(OnReadyLastRun) when data.member is IMethodSymbol methodSymbol
                    => new LastRun(methodSymbol.Name),
                _ => new OnReadyAction(),
            };
            actionList.Add(actionInfo);
        }

        return new Root(classSymbol.ContainingNamespace.Name, classSymbol.Name, actionList);
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<Root> array)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var getNodeStatement = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<Get>()
                    .Select(
                        v =>
                            $"{v.FieldName} = GetNode<{(v.FieldTypeNamespace == "" ? "" : $"{v.FieldTypeNamespace}.")}{v.FieldType}>(\"{v.FieldPath}\");"
                    )
            );
            var connectSignalStatement = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<Connect>()
                    .Select(
                        v =>
                            $"{(v.Source.Length == 0 ? "" : $"{v.Source}.")}{v.Signal} += {v.MethodName};"
                    )
            );

            var runStatement = string.Join(
                "\n        ",
                info.ActionList
                    .OfType<Run>()
                    .OrderBy(v => v.Order)
                    .Select(v => $"{v.MethodName}();")
            );
            var lastRunStatement = string.Join(
                "\n        ",
                info.ActionList.OfType<LastRun>().Select(v => $"{v.MethodName}();")
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
        {runStatement}
        {lastRunStatement}
    }} 
}}
"
            );
        }
    }

    record Root(string Namespace, string ClassName, List<OnReadyAction> ActionList);

    record OnReadyAction;

    record Get(string FieldName, string FieldType, string? _fieldPath, string FieldTypeNamespace)
        : OnReadyAction
    {
        public string FieldPath => _fieldPath ?? _uniqueName;

        private string _uniqueName =>
            FieldName.StartsWith("_")
                ? $"%{FieldName[1].ToString().ToUpper()}{FieldName.Substring(2)}"
                : $"%{FieldName}";
    }

    record Connect(string MethodName, string Source, string Signal) : OnReadyAction;

    record Run(string MethodName, int Order) : OnReadyAction;

    record LastRun(string MethodName) : OnReadyAction;

    class RootEqual : IEqualityComparer<Root>
    {
        public bool Equals(Root? x, Root? y)
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

        public int GetHashCode(Root obj)
        {
            return HashCode.Combine(
                obj.Namespace,
                obj.ClassName,
                obj.ActionList.GetSequenceHashCode()
            );
        }
    }
}
