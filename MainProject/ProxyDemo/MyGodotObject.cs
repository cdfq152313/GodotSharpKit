using Godot;
using Godot.Collections;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[GodotProxy]
public interface IMyGodotObject
{
    [GodotProxyName("x")]
    int X { get; set; }
    int GetterOnly { get; }
    string SetterOnly { set; }

    [GodotProxyName("print_setter_only")]
    void PrintSetterOnly();
    void MyMethod(int a, Node b);
    string MyMethodAndReturnValue(int a, Node b);

    void EmitAll();

    [GodotProxyName("my_signal")]
    delegate void MySignalEventHandler();
    delegate void MySignalParamEventHandler(int a, Node b);
    delegate void MySignalParamWithGenericEventHandler(Array<int> x);
}

public partial class MyGodotObject : IMyGodotObject { }
