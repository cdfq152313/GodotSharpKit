using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using GodotSharpKit.Misc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotSharpKit.Generator;

[Generator(LanguageNames.CSharp)]
public class ProxyGenerator : IIncrementalGenerator
{
    private static Regex _pascal = new(@"(\B[A-Z])");

    private static string ToSnake(string input, bool apply)
    {
        if (string.IsNullOrEmpty(input) || !apply)
        {
            return input;
        }
        return _pascal.Replace(input, "_$1").ToLower().TrimStart('_');
    }

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
        var godotProxyAttribute = interfaceSymbol
            .GetAttributes()
            .First(v => v.AttributeClass?.FullName() == typeof(GodotProxy).FullName);
        var autoSnakeCase = godotProxyAttribute.ConstructorArguments[0].Value as bool? ?? false;
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
                        autoSnakeCase,
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
                        autoSnakeCase,
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
                        autoSnakeCase,
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
            var signalDeclaration = SignalDeclaration(info.MemberList.OfType<Signal>().ToList());
            context.AddSource(
                $"{info.Namespace.ConcatDot(info.ClassName).Replace(".", "_")}.g.cs",
                @$"{namespaceStatement}
            
public partial class {info.ClassName} : I{info.ClassName}
{{
    public {info.ClassName}(Godot.GodotObject obj)
    {{
        GodotObject = obj;
    }}

    public Godot.GodotObject GodotObject;

{declaration}
{signalDeclaration}
}}
            "
            );
        }
    }

    private string SignalDeclaration(List<Signal> signals)
    {
        if (signals.Count == 0)
        {
            return "";
        }
        var sb = new StringBuilder();
        sb.AppendIndent();
        sb.AppendLine("public class SignalName");
        sb.AppendIndent();
        sb.AppendLine("{");
        foreach (var signal in signals)
        {
            sb.AppendIndent(2);
            sb.AppendLine(
                $"public static readonly Godot.StringName {signal.CSharpName} = (Godot.StringName) \"{signal.GodotName ?? ToSnake(signal.CSharpName, signal.ApplyToSnake)}\";"
            );
        }
        sb.AppendIndent();
        sb.AppendLine("}");
        return sb.ToString();
    }

    record Root(string Namespace, string ClassName, List<Member> MemberList);

    record Member
    {
        public virtual string ToDeclaration() => "";
    }

    record Property(
        string CSharpName,
        string? GodotName,
        bool ApplyToSnake,
        string Type,
        bool HasGetter,
        bool HasSetter
    ) : Member
    {
        public override string ToDeclaration()
        {
            var sb = new StringBuilder();
            var godotName = GodotName ?? ToSnake(CSharpName, ApplyToSnake);
            sb.AppendIndent();
            sb.AppendLine($"public {Type} {CSharpName}");
            sb.AppendIndent();
            sb.AppendLine("{");
            if (HasGetter)
            {
                sb.AppendIndent(2);
                sb.AppendLine($"get => ({Type}) GodotObject.Get(\"{godotName}\");");
            }
            if (HasSetter)
            {
                sb.AppendIndent(2);
                sb.AppendLine($"set => GodotObject.Set(\"{godotName}\", value);");
            }
            sb.AppendIndent();
            sb.Append("}");
            return sb.ToString();
        }
    }

    record Method(
        string CSharpName,
        string? GodotName,
        bool ApplyToSnake,
        string? Return,
        List<Param> Params
    ) : Member
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
                new List<string> { $"\"{GodotName ?? ToSnake(CSharpName, ApplyToSnake)}\"" }.Concat(
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

    record Signal(string CSharpName, string? GodotName, bool ApplyToSnake, List<Param> Params)
        : Member
    {
        public override string ToDeclaration()
        {
            var sb = new StringBuilder();
            var godotName = GodotName ?? ToSnake(CSharpName, ApplyToSnake);
            AppendListener(sb);
            AppendEmitter(sb);
            AppendAwaiter(sb);
            return sb.ToString();
        }

        private void AppendListener(StringBuilder sb)
        {
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
                $"add => GodotObject.Connect(SignalName.{CSharpName}, Godot.Callable.From(value));"
            );
            sb.AppendIndent(2);
            sb.AppendLine(
                $"remove => GodotObject.Disconnect(SignalName.{CSharpName}, Godot.Callable.From(value));"
            );
            sb.AppendIndent();
            sb.AppendLine("}");
        }

        private void AppendEmitter(StringBuilder sb)
        {
            sb.AppendIndent();
            sb.AppendLine(
                $"public Godot.SignalAwaiter ToSignal{CSharpName}(Godot.GodotObject user)"
            );
            sb.AppendIndent();
            sb.AppendLine("{");
            sb.AppendIndent(2);
            sb.AppendLine($"return user.ToSignal(GodotObject, SignalName.{CSharpName});");
            sb.AppendIndent();
            sb.AppendLine("}");
        }

        private void AppendAwaiter(StringBuilder sb)
        {
            sb.AppendIndent();
            var parameters = string.Join(", ", Params.Select(v => $"{v.Type} {v.Name}"));
            sb.AppendLine($"public void EmitSignal{CSharpName}({parameters})");
            sb.AppendIndent();
            sb.AppendLine("{");
            sb.AppendIndent(2);
            var args =
                Params.Count == 0 ? "" : $", {string.Join(", ", Params.Select(v => v.Name))}";
            sb.AppendLine($"GodotObject.EmitSignal(SignalName.{CSharpName}{args});");
            sb.AppendIndent();
            sb.AppendLine("}");
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
