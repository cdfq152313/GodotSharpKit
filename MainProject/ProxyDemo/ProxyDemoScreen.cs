using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace Godot4Demo.ProxyDemo;

public partial class ProxyDemoScreen : Node2D
{
    private MyGodotObject _myObj = null!;

    public override void _Ready()
    {
        var script = GD.Load<GDScript>("res://ProxyDemo/MyGodotObject.gd");

        // check x
        GD.Print("=== Property ===");
        _myObj = new MyGodotObject((GodotObject)script.New());
        GD.Print($"X: {_myObj.X}");
        _myObj.X = 100;
        GD.Print($"X: {_myObj.X}");

        // check getter only
        _myObj = new MyGodotObject((GodotObject)script.New());
        GD.Print($"GetterOnly: {_myObj.GetterOnly}");

        // check setter only
        _myObj.SetterOnly = "3310";
        _myObj.PrintSetterOnly();

        // method with parameter
        GD.Print("=== Method ===");
        _myObj.MyMethod(1, new Node());
        var x = _myObj.MyMethodAndReturnValue(1, new Node());
        GD.Print($"Return Value: {x}");

        // connect signal
        GD.Print("=== Signal ===");
        _myObj.MySignal += MyObjOnMySignal;
        _myObj.MySignalParam += MyObjOnMySignalParam;
        _myObj.MySignalParamWithGeneric += MyObjOnMySignalParamWithGeneric;
        Awaiter(_myObj);
        _myObj.EmitSignalMySignal();
        _myObj.EmitSignalMySignalParam(1, new Node());
        _myObj.EmitSignalMySignalParamWithGeneric(new Array<int> { 1, 2, 3, 4 });

        // cancel signal
        _myObj.MySignal -= MyObjOnMySignal;
        _myObj.MySignalParam -= MyObjOnMySignalParam;
        _myObj.MySignalParamWithGeneric -= MyObjOnMySignalParamWithGeneric;
        _myObj.EmitSignalMySignal();
        _myObj.EmitSignalMySignalParam(1, new Node());
        _myObj.EmitSignalMySignalParamWithGeneric(new Array<int> { 1, 2, 3, 4 });
    }

    private async Task Awaiter(MyGodotObject myObj)
    {
        await myObj.ToSignalMySignal(this);
        await myObj.ToSignalMySignalParam(this);
        await myObj.ToSignalMySignalParamWithGeneric(this);
        GD.Print("Awaiter End");
    }

    private void MyObjOnMySignal()
    {
        GD.Print("GodotObjectOnMySignal");
    }

    private void MyObjOnMySignalParam(int arg1, Node arg2)
    {
        GD.Print($"GodotObjectOnMySignalParam: {arg1}/{arg2}");
    }

    private void MyObjOnMySignalParamWithGeneric(Array<int> obj)
    {
        GD.Print($"GodotObjectOnMySignalParamWithGeneric: {obj}");
    }
}
