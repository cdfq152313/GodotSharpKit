using System.Threading.Tasks;
using Godot;

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
        AsyncAction();
        EmitMySignal();
        EmitMySignalParam(1, new Node());
        EmitMySignalParamWithGeneric(new MyGeneric<int>());
    }

    private async Task AsyncAction()
    {
        var x = await ToSignalMySignal(this);
        GD.Print("Async Action");
    }
}
