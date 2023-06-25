using System;
using GdExtension;
using Godot;
using Godot4Demo.Inner;

namespace Godot4Demo;

[OnReadyClass]
public partial class LaunchScreen : Node2D
{
    [Signal]
    public delegate void MySignalEventHandler();

    [OnReadyNode]
    private CustomNode _hello;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        OnReady();
        GD.Print("Hello Ready!");
        MySignal += () => GD.Print("Hello!");
        EmitSignal(SignalName.MySignal);
        Console.WriteLine(typeof(OnReadyNode).FullName);
        new HelloGenerator();
        new HelloIncrementalGenerator();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
