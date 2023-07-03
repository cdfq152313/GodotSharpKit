using System.Collections.Immutable;
using System.Reflection;
using GdExtension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests;

public class GeneratorTest
{
    [SetUp]
    public void Setup() { }

    private string GetRootPath()
    {
        var dir = Environment.CurrentDirectory;
        while (!Directory.Exists(Path.Join(dir, "GdExtension")))
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
        var cs = File.ReadAllText(Path.Join(root, "MainProject", "LaunchScreen.cs"));
        var configContent = File.ReadAllText(
            Path.Join(root, "MainProject", "gdExtension.config.json")
        );
        Compilation inputCompilation = CreateCompilation(cs);

        var generator = new ResourceGenerator();
        CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(
                ImmutableArray.Create<AdditionalText>(
                    new InMemoryAdditionalText("gdExtension.config.json", configContent)
                )
            )
            .RunGenerators(inputCompilation);
    }

    private static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
            }
        );
}
