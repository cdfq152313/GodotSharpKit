using Godot;
using Godot.Collections;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[GodotProxy]
public interface IMyGodotObject
{
    int X { get; set; }
    int GetterOnly { get; }
    string SetterOnly { set; }
    void PrintSetterOnly();
    void MyMethod(int a, Node b);
    string MyMethodAndReturnValue(int a, Node b);

    void EmitAll();
    delegate void MySignalEventHandler();
    delegate void MySignalParamEventHandler(int a, Node b);
    delegate void MySignalParamWithGenericEventHandler(Array<int> x);
}

public partial class MyGodotObject : IMyGodotObject { }
