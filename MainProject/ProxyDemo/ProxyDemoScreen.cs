using Godot;
using Godot.Collections;

namespace Godot4Demo.ProxyDemo;

public partial class ProxyDemoScreen : Node2D
{
    private MyGodotObject _godotObject = null!;

    public override void _Ready()
    {
        var script = GD.Load<GDScript>("res://ProxyDemo/MyGodotObject.gd");

        // check x
        _godotObject = new MyGodotObject((GodotObject)script.New());
        GD.Print($"X: {_godotObject.X}");
        _godotObject.X = 100;
        GD.Print($"X: {_godotObject.X}");

        // check getter only
        GD.Print($"GetterOnly: {_godotObject.GetterOnly}");

        // check setter only
        _godotObject.SetterOnly = "3310";
        _godotObject.PrintSetterOnly();

        // method with parameter
        _godotObject.MyMethod(1, new Node());
        var x = _godotObject.MyMethodAndReturnValue(1, new Node());
        GD.Print($"Return Value: {x}");

        // connect signal
        _godotObject.MySignal += GodotObjectOnMySignal;
        _godotObject.MySignalParam += GodotObjectOnMySignalParam;
        _godotObject.MySignalParamWithGeneric += GodotObjectOnMySignalParamWithGeneric;
        _godotObject.EmitAll();

        // cancel signal
        _godotObject.MySignal -= GodotObjectOnMySignal;
        _godotObject.MySignalParam -= GodotObjectOnMySignalParam;
        _godotObject.MySignalParamWithGeneric -= GodotObjectOnMySignalParamWithGeneric;
        _godotObject.EmitAll();
    }

    private void GodotObjectOnMySignal()
    {
        GD.Print("GodotObjectOnMySignal");
    }

    private void GodotObjectOnMySignalParam(int arg1, Node arg2)
    {
        GD.Print($"GodotObjectOnMySignalParam: {arg1}/{arg2}");
    }

    private void GodotObjectOnMySignalParamWithGeneric(Array<int> obj)
    {
        GD.Print($"GodotObjectOnMySignalParamWithGeneric: {obj}");
    }
}
