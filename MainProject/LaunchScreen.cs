using System;
using GdExtension;
using Godot;
using Godot4Demo.Inner;

namespace Godot4Demo;

[GdExtNode]
public partial class LaunchScreen : Node2D
{
    [Signal]
    public delegate void MySignalEventHandler();

    [OnReadyNode]
    private CustomNode _node1;

    [OnReadyNode("haha")]
    private Node _node2;

    private Timer _timer = new();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        OnReady();
        GD.Print("Hello Ready!");
        MySignal += () => GD.Print("Hello!");
        EmitSignal(SignalName.MySignal);
        Console.WriteLine(typeof(OnReadyNode).FullName);
    }

    [OnReadyConnect("", nameof(MySignal))]
    private void OnMySignal() { }

    [OnReadyConnect(nameof(_timer), nameof(Timer.Timeout))]
    private void OnTimeout() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
