using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotSharpKit.Generator;

[Generator(LanguageNames.CSharp)]
public class SignalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Godot.SignalAttribute",
            IsSyntaxTarget,
            GetSyntaxTarget
        );
        context.RegisterSourceOutput(syntaxProvider.Collect(), OnExecute);
    }

    private bool IsSyntaxTarget(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is DelegateDeclarationSyntax syntax
            && syntax.Identifier.Text.EndsWith("EventHandler");
    }

    private DelegateInfo GetSyntaxTarget(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var delegateDeclarationSyntax = (DelegateDeclarationSyntax)context.TargetNode;
        var signalParams = new List<ParamInfo>();
        foreach (var p in delegateDeclarationSyntax.ParameterList.Parameters)
        {
            var typeSymbol = context.SemanticModel.GetSymbolInfo(p.Type!).Symbol!;
            signalParams.Add(new ParamInfo(typeSymbol.FullName(), p.Identifier.Text));
        }
        return new DelegateInfo(
            context.TargetSymbol.ContainingNamespace.FullName(),
            context.TargetSymbol.ContainingType.Name,
            context.TargetSymbol.Name.Replace("EventHandler", ""),
            signalParams
        );
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<DelegateInfo> array)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var param = string.Join(",", info.SignalParams.Select(v => $"{v.Type} {v.Name}"));
            var passParam =
                info.SignalParams.Count > 0
                    ? $",{string.Join(",", info.SignalParams.Select(v => v.Name))}"
                    : "";
            context.AddSource(
                $"{info.Namespace.ConcatDot(info.ClassName).Replace(".", "_")}_Emit{info.SignalName}.g.cs",
                @$"{namespaceStatement}
public partial class {info.ClassName} 
{{ 
    public void Emit{info.SignalName}({param})
    {{
        EmitSignal(SignalName.{info.SignalName}{passParam});
    }} 
}}
"
            );
        }
    }

    record DelegateInfo(
        string Namespace,
        string ClassName,
        string SignalName,
        List<ParamInfo> SignalParams
    );

    record ParamInfo(string Type, string Name);
}
