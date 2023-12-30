using System.Collections.Immutable;
using System.Text;
using GodotSharpKit.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotSharpKit.Generator;

[Generator(LanguageNames.CSharp)]
public class OnReadyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(OnReady).FullName!,
            IsSyntaxTarget,
            GetSyntaxTarget
        );
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
            select new { member, attribute };
        var actionList = new SeqList<OnReadyAction>();
        bool hasDisposable = false;
        foreach (var data in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                data.attribute.AttributeClass!.ContainingNamespace!.FullName()
                != typeof(OnReadyGet).Namespace
            )
            {
                continue;
            }
            var actionInfo = data.attribute.AttributeClass!.Name switch
            {
                nameof(OnReadyGet) when data.member is IFieldSymbol fieldSymbol
                    => new Get(
                        fieldSymbol.Name,
                        fieldSymbol.Type.Name,
                        data.attribute.ConstructorArguments[0].Value as string,
                        fieldSymbol.Type.ContainingNamespace.FullName()
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
                        (int)data.attribute.ConstructorArguments[0].Value!,
                        methodSymbol.ReturnType.Name == nameof(IDisposable)
                    ),
                _ => new OnReadyAction(),
            };
            if (actionInfo is Run { IsDisposable: true })
            {
                hasDisposable = true;
            }
            actionList.Add(actionInfo);
        }

        return new Root(
            classSymbol.ContainingNamespace.FullName(),
            classSymbol.Name,
            hasDisposable,
            actionList
        );
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<Root> array)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var onReadyStatementBuilder = new StringBuilder();
            var orderActionList = new List<OnReadyAction>()
                .Concat(info.ActionList.OfType<Get>())
                .Concat(info.ActionList.OfType<Connect>())
                .Concat(info.ActionList.OfType<Run>().OrderBy(v => v.Order));

            foreach (var action in orderActionList)
            {
                onReadyStatementBuilder.AppendLine();
                context.CancellationToken.ThrowIfCancellationRequested();
                onReadyStatementBuilder.AppendIndent(2);
                onReadyStatementBuilder.Append(action.OnReadyStatement());
            }

            context.AddSource(
                $"{info.Namespace.ConcatDot(info.ClassName).Replace(".", "_")}.g.cs",
                @$"{namespaceStatement}
using System;
using System.Collections.Generic;

public partial class {info.ClassName} 
{{ 
    private void OnReady()
    {{{onReadyStatementBuilder}
    }} 
    
    {DisposableExpression(info.HasDisposable)}
}}
"
            );
        }
    }

    private string DisposableExpression(bool hasDisposable)
    {
        return hasDisposable
            ? @"private List<IDisposable> _disposables = new List<IDisposable>();

    private void OnDispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }"
            : "";
    }

    record Root(
        string Namespace,
        string ClassName,
        bool HasDisposable,
        SeqList<OnReadyAction> ActionList
    );

    record OnReadyAction
    {
        public virtual string OnReadyStatement()
        {
            return "";
        }
    }

    record Get(string FieldName, string FieldType, string? _fieldPath, string FieldTypeNamespace)
        : OnReadyAction
    {
        public string FieldPath => _fieldPath ?? _uniqueName;

        private string _uniqueName =>
            FieldName.StartsWith("_")
                ? $"%{FieldName[1].ToString().ToUpper()}{FieldName.Substring(2)}"
                : $"%{FieldName}";

        public override string OnReadyStatement()
        {
            return $"{FieldName} = GetNode<{(FieldTypeNamespace == "" ? "" : $"{FieldTypeNamespace}.")}{FieldType}>(\"{FieldPath}\");";
        }
    }

    record Connect(string MethodName, string Source, string Signal) : OnReadyAction
    {
        public override string OnReadyStatement()
        {
            return $"{(Source.Length == 0 ? "" : $"{Source}.")}{Signal} += {MethodName};";
        }
    }

    record Run(string MethodName, int Order, bool IsDisposable) : OnReadyAction
    {
        public override string OnReadyStatement()
        {
            return IsDisposable ? $"_disposables.Add({MethodName}());" : $"{MethodName}();";
        }
    }
}
