using Godot;
using GodotSharpKit.Misc;

namespace Godot4Demo.SignalDemo;

public partial class SignalDemoScreen : Node2D
{
    public partial class MyGeneric<T> : RefCounted { }

    [Signal]
    public delegate void MySignalEventHandler();

    [Signal]
    public delegate void MySignalParamEventHandler(int a, Node b);

    [Signal]
    public delegate void MySignalParamWithGenericEventHandler(MyGeneric<int> x);

    public override void _Ready()
    {
        MySignal += () => GD.Print("Hello!");
        MySignalParam += (a, b) => GD.Print($"Hello! {a} {b}");
        MySignalParamWithGeneric += (x) => GD.Print($"Hello! {x}");
        EmitMySignal();
        EmitMySignalParam(1, new Node());
        EmitMySignalParamWithGeneric(new MyGeneric<int>());
    }
}
