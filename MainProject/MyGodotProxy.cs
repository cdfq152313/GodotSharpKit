using Godot;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[GodotProxy]
public interface IMyGodotProxy
{
    string X { get; set; }
    void MyMethod();
    void MyMethodWithParam();
    delegate void MySignalEventHandler();
    delegate void MySignalParamEventHandler(int a, Node b);
    delegate void MySignalParamWithGenericEventHandler(LaunchScreen.MyGeneric<int> x);
}

// public partial class MyGodotProxy : IMyGodotProxy { }
