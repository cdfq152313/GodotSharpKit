using System;
using GdExtension;
using Godot;
using Godot4Demo.Inner;

namespace Godot4Demo;

public partial class LaunchScreen : Node2D
{
    [Signal]
    public delegate void MySignalEventHandler();

    [OnReadyNode]
    private CustomNode _hello;

    private Timer _timer = new();

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

    [OnReadyConnect(nameof(MySignal))]
    private void OnMySignalTrigger() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
