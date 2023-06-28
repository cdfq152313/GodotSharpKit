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
    public void DebugGenerator()
    {
        var root = GetRootPath();
        var cs = File.ReadAllText(Path.Join(root, "MainProject", "LaunchScreen.cs"));
        Compilation inputCompilation = CreateCompilation(cs);
        var generator = new OnReadyGenerator();

        // Create the driver that will control the generation, passing in our generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
        driver = driver.RunGeneratorsAndUpdateCompilation(
            inputCompilation,
            out var outputCompilation,
            out var diagnostics
        );
        var runResult = driver.GetRunResult();
        foreach (var result in runResult.Results)
        {
            Assert.That(result.Exception, Is.Null);
        }
    }

    private static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );
}
