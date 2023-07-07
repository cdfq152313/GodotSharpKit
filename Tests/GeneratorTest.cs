using System.Collections.Immutable;
using System.Reflection;
using GodotSharpKit;
using GodotSharpKit.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq;

namespace Tests;

public class GeneratorTest
{
    [SetUp]
    public void Setup() { }

    private string GetRootPath()
    {
        var dir = Environment.CurrentDirectory;
        while (!Directory.Exists(Path.Join(dir, "GodotSharpKit")))
        {
            dir = Directory.GetParent(dir)!.FullName;
        }

        return dir;
    }

    [Test]
    public void DebugOnReadyGenerator()
    {
        var root = GetRootPath();
        var cs = File.ReadAllText(Path.Join(root, "MainProject", "LaunchScreen.cs"));
        Compilation inputCompilation = CreateCompilation(cs);
        var generator = new OnReadyGenerator();
        CSharpGeneratorDriver.Create(generator).RunGenerators(inputCompilation);
    }

    [Test]
    public void DebugResourceGenerator()
    {
        var root = GetRootPath();
        var mainProjectPath = Path.Join(root, "MainProject");
        var cs = File.ReadAllText(Path.Join(mainProjectPath, "LaunchScreen.cs"));
        var path = Path.Join(mainProjectPath, "Inner", "CustomNode.tscn");
        var content = File.ReadAllText(path);
        Compilation inputCompilation = CreateCompilation(cs);
        var generator = new ResourceGenerator();
        var mockOptions = new Mock<AnalyzerConfigOptionsProvider>();
        var empty = "";
        mockOptions.Setup(
            v => v.GlobalOptions.TryGetValue("build_property.projectdir", out mainProjectPath)
        );
        mockOptions.Setup(
            v => v.GetOptions(It.IsAny<AdditionalText>()).TryGetValue(It.IsAny<string>(), out empty)
        );
        CSharpGeneratorDriver
            .Create(generator)
            .WithUpdatedAnalyzerConfigOptions(mockOptions.Object)
            .AddAdditionalTexts(
                ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText(path, content))
            )
            .RunGenerators(inputCompilation);
    }

    private static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
            }
        );
}
