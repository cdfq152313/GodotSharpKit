using System.Collections.Immutable;
using System.Text;
using GodotSharpKit.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotSharpKit.Generator;

[Generator(LanguageNames.CSharp)]
public class ProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                typeof(GodotProxy).FullName!,
                IsSyntaxTarget,
                GetSyntaxTarget
            )
            .WithComparer(new RootEqual());
        context.RegisterSourceOutput(syntaxProvider.Collect(), OnExecute);
    }

    private bool IsSyntaxTarget(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is InterfaceDeclarationSyntax;
    }

    private Root GetSyntaxTarget(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var interfaceSymbol = (ITypeSymbol)context.TargetSymbol;
        var actionList = new List<Member>();
        foreach (var member in interfaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nameAttribute = member
                .GetAttributes()
                .FirstOrDefault(
                    v => v.AttributeClass?.FullName() == typeof(GodotProxyName).FullName
                );
            var godotName = nameAttribute?.ConstructorArguments[0].Value as string;
            Member action = member switch
            {
                IPropertySymbol propertySymbol
                    => new Property(
                        propertySymbol.Name,
                        godotName,
                        propertySymbol.Type.FullName(),
                        propertySymbol.GetMethod != null,
                        propertySymbol.SetMethod != null
                    ),
                IMethodSymbol methodSymbol
                    when methodSymbol.MethodKind != MethodKind.PropertyGet
                        && methodSymbol.MethodKind != MethodKind.PropertySet
                    => new Method(
                        methodSymbol.Name,
                        godotName,
                        methodSymbol.ReturnsVoid ? null : methodSymbol.ReturnType.FullName(),
                        methodSymbol.Parameters
                            .Select(v => new Param(v.Type.FullName(), v.Name))
                            .ToList()
                    ),
                INamedTypeSymbol { DelegateInvokeMethod: not null } namedTypeSymbol
                    when namedTypeSymbol.Name.Contains("EventHandler")
                    => new Signal(
                        namedTypeSymbol.Name.Replace("EventHandler", ""),
                        godotName,
                        namedTypeSymbol.DelegateInvokeMethod.Parameters
                            .Select(v => new Param(v.Type.FullName(), v.Name))
                            .ToList()
                    ),
                _ => new NotImplement(),
            };
            if (action is not NotImplement)
            {
                actionList.Add(action);
            }
        }

        return new Root(
            interfaceSymbol.ContainingNamespace.FullName(),
            interfaceSymbol.Name.Substring(1), // Eliminate 'I'
            actionList
        );
    }

    private void OnExecute(SourceProductionContext context, ImmutableArray<Root> array)
    {
        foreach (var info in array)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var namespaceStatement = info.Namespace == "" ? "" : $"namespace {info.Namespace};";
            var declaration = string.Join("\n\n", info.MemberList.Select(v => v.ToDeclaration()));
            context.AddSource(
                $"{info.Namespace.ConcatDot(info.ClassName).Replace(".", "_")}.g.cs",
                @$"{namespaceStatement}
            
public partial class {info.ClassName}
{{
    public {info.ClassName}(Godot.GodotObject obj)
    {{
        GodotObject = obj;
    }}

    public Godot.GodotObject GodotObject;

    {declaration}
}}
            "
            );
        }
    }

    record Root(string Namespace, string ClassName, List<Member> MemberList);

    record Member
    {
        public virtual string ToDeclaration() => "";
    }

    record Property(
        string CSharpName,
        string? GodotName,
        string Type,
        bool HasGetter,
        bool HasSetter
    ) : Member
    {
        public override string ToDeclaration()
        {
            var sb = new StringBuilder();
            sb.AppendIndent();
            sb.AppendLine($"public {Type} {CSharpName}");
            sb.AppendIndent();
            sb.AppendLine("{");
            if (HasGetter)
            {
                sb.AppendIndent(2);
                sb.AppendLine($"get => ({Type}) GodotObject.Get(\"{GodotName ?? CSharpName}\");");
            }
            if (HasSetter)
            {
                sb.AppendIndent(2);
                sb.AppendLine($"set => GodotObject.Set(\"{GodotName ?? CSharpName}\", value);");
            }
            sb.AppendIndent();
            sb.Append("}");
            return sb.ToString();
        }
    }

    record Method(string CSharpName, string? GodotName, string? Return, List<Param> Params) : Member
    {
        public override string ToDeclaration()
        {
            var sb = new StringBuilder();
            var parameters = string.Join(",", Params.Select(v => $"{v.Type} {v.Name}"));
            sb.AppendIndent();
            sb.AppendLine($"public {Return ?? "void"} {CSharpName}({parameters})");
            sb.AppendIndent();
            sb.AppendLine("{");

            var callArgs = string.Join(
                ", ",
                new List<string> { $"\"{GodotName ?? CSharpName}\"" }.Concat(
                    Params.Select(v => v.Name)
                )
            );
            var callStatement = $"GodotObject.Call({callArgs});";
            sb.AppendIndent(times: 2);
            if (Return != null)
            {
                sb.Append($"return ({Return}) ");
            }
            sb.AppendLine(callStatement);
            sb.AppendIndent();
            sb.Append("}");
            return sb.ToString();
        }
    }

    record Signal(string CSharpName, string? GodotName, List<Param> Params) : Member
    {
        public override string ToDeclaration()
        {
            var sb = new StringBuilder();
            sb.AppendIndent();
            var action =
                Params.Count == 0
                    ? "System.Action"
                    : $"System.Action<{string.Join(", ", Params.Select(v => v.Type))}>";
            sb.AppendLine($"public event {action} {CSharpName}");
            sb.AppendIndent();
            sb.AppendLine("{");
            sb.AppendIndent(2);
            sb.AppendLine(
                $"add => GodotObject.Connect(\"{GodotName ?? CSharpName}\", Godot.Callable.From(value));"
            );
            sb.AppendIndent(2);
            sb.AppendLine(
                $"remove => GodotObject.Disconnect(\"{GodotName ?? CSharpName}\", Godot.Callable.From(value));"
            );
            sb.AppendIndent();
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    record NotImplement : Member;

    record Param(string Type, string Name);

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
                && x.MemberList.SequenceEqual(y.MemberList);
        }

        public int GetHashCode(Root obj)
        {
            return (
                obj.Namespace,
                obj.ClassName,
                obj.MemberList.GetSequenceHashCode()
            ).GetHashCode();
        }
    }
}
