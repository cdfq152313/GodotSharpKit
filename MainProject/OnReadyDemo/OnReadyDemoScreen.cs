using System;
using System.Collections.Generic;
using Godot;
using Godot4Demo.OnReadyDemo.Inner;
using GodotSharpKit.Misc;

namespace Godot4Demo.OnReadyDemo;

[OnReady]
public partial class OnReadyDemoScreen : Node2D
{
    [OnReadyGet]
    private CustomNode _node1 = null!;

    [OnReadyGet]
    public Node Node2 = null!;

    [OnReadyGet("haha")]
    private Node _node3 = null!;

    [OnReadyGet]
    private Timer _timer = null!;

    public override void _Ready()
    {
        base._Ready();
        OnReady();
    }

    [OnReadyConnect(nameof(_timer), nameof(Timer.Timeout))]
    private void OnTimeout()
    {
        GD.Print("Timeout!!!");
    }

    [OnReadyRun(2)]
    private void Run2()
    {
        GD.Print("Two!");
    }

    [OnReadyRun(1)]
    private IDisposable Run1()
    {
        GD.Print("One!");
        return new MyDisposable();
    }

    [OnReadyRun(3)]
    private List<IDisposable> Run3()
    {
        GD.Print("Three!");
        return new List<IDisposable>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        OnDispose();
    }

    class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            GD.Print("MyDisposable");
        }
    }
}
