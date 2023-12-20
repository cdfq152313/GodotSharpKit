using Godot;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[GodotProxy]
public interface IMyGodotProxy
{
    string X { get; set; }
    int GetterOnly { get; }
    string SetterOnly { set; }
    void MyMethod();
    void MyMethodWithParam(int a, Node b);
    string MyMethodWithParamAndReturnValue(int a, Node b);
    delegate void MySignalEventHandler();
    delegate void MySignalParamEventHandler(int a, Node b);
    delegate void MySignalParamWithGenericEventHandler(LaunchScreen.MyGeneric<int> x);
}

public partial class MyGodotProxy : IMyGodotProxy
{
    void XXX() { }
}
