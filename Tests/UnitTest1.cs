using Godot;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        int i = 0;
        Console.WriteLine("Testsss");
        GD.Print("NUnitTest");
        var main = GD.Load<PackedScene>("res://LaunchScreen.tscn").Instantiate()!;
        Assert.That(main.Name.ToString(), Is.EqualTo("LaunchScreen"));
    }
}