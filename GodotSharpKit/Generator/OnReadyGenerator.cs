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
        var getList = new SeqList<Get>();
        var connectList = new SeqList<Connect>();
        var runList = new SeqList<Run>();
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
            switch (data.attribute.AttributeClass!.Name)
            {
                case nameof(OnReadyGet) when data.member is IFieldSymbol fieldSymbol:
                    getList.Add(
                        new Get(
                            fieldSymbol.Name,
                            fieldSymbol.Type.Name,
                            data.attribute.ConstructorArguments[0].Value as string,
                            fieldSymbol.Type.ContainingNamespace.FullName()
                        )
                    );
                    break;
                case nameof(OnReadyConnect) when data.member is IMethodSymbol methodSymbol:
                    connectList.Add(
                        new Connect(
                            methodSymbol.Name,
                            (string)data.attribute.ConstructorArguments[0].Value!,
                            (string)data.attribute.ConstructorArguments[1].Value!
                        )
                    );
                    break;
                case nameof(OnReadyRun) when data.member is IMethodSymbol methodSymbol:
                    runList.Add(
                        new Run(
                            methodSymbol.Name,
                            (int)data.attribute.ConstructorArguments[0].Value!,
                            methodSymbol.ReturnType switch
                            {
                                { Name: nameof(IDisposable) } => Disposable.Single,
                                INamedTypeSymbol
                                {
                                    Name: "List",
                                    TypeArguments.Length: > 0
                                } namedTypeSymbol
                                    when namedTypeSymbol.TypeArguments.First().Name
                                        == nameof(IDisposable)
                                    => Disposable.List,
                                _ => Disposable.None,
                            }
                        )
                    );
                    break;
            }
        }

        return new Root(
            classSymbol.ContainingNamespace.FullName(),
            classSymbol.Name,
            getList,
            connectList,
            runList
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
                .Concat(info.GetList)
                .Concat(info.ConnectList)
                .Concat(info.RunList.OrderBy(v => v.Order));

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
    
    {DisposableExpression(info.RunList.Any(v => v.Disposable != Disposable.None))}
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
        SeqList<Get> GetList,
        SeqList<Connect> ConnectList,
        SeqList<Run> RunList
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

    record Run(string MethodName, int Order, Disposable Disposable) : OnReadyAction
    {
        public override string OnReadyStatement()
        {
            return Disposable switch
            {
                Disposable.None => $"{MethodName}();",
                Disposable.Single => $"_disposables.Add({MethodName}());",
                Disposable.List => $"_disposables.AddRange({MethodName}());",
            };
        }
    }

    public enum Disposable
    {
        None,
        Single,
        List
    }
}
