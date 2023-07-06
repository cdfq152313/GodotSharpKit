using System;
using GodotSharpKit;
using Godot;
using Godot4Demo.Inner;

namespace Godot4Demo;

[OnReady]
public partial class LaunchScreen : Node2D
{
    [Signal]
    public delegate void MySignalEventHandler();

    [OnReadyGet]
    private CustomNode _node1;

    [OnReadyGet("haha")]
    private Node _node2;

    private Timer _timer = new();

    [OnReadyLastRun]
    private void PostReady()
    {
        MySignal += () => GD.Print("Hello!");
        EmitSignal(SignalName.MySignal);
        Console.WriteLine(typeof(OnReadyGet).FullName);
    }

    [OnReadyConnect("", nameof(MySignal))]
    private void OnMySignal() { }

    [OnReadyConnect(nameof(_timer), nameof(Timer.Timeout))]
    private void OnTimeout() { }

    [OnReadyRun(2)]
    private void Run2()
    {
        GD.Print("Two!");
    }

    [OnReadyRun(1)]
    private void Run1()
    {
        GD.Print("One!");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
