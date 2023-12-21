using Godot;
using Godot.Collections;
using GodotSharpKit.Misc;

namespace Godot4Demo.ProxyDemo;

[GodotProxy]
public interface IMyGodotObject
{
    [GodotProxyName("X")]
    int X { get; set; }
    int GetterOnly { get; }
    string SetterOnly { set; }

    [GodotProxyName("PrintSetterOnly")]
    void PrintSetterOnly();
    void MyMethod(int a, Node b);
    string MyMethodAndReturnValue(int a, Node b);

    [GodotProxyName("MySignal")]
    delegate void MySignalEventHandler();
    delegate void MySignalParamEventHandler(int a, Node b);
    delegate void MySignalParamWithGenericEventHandler(Array<int> x);
}

public partial class MyGodotObject { }
