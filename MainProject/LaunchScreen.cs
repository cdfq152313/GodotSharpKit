using Godot;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[OnReady]
public partial class LaunchScreen : Control
{
    [OnReadyGet]
    private Button _onReadyDemo = null!;

    [OnReadyGet]
    private Button _autoResDemo = null!;

    [OnReadyGet]
    private Button _signalDemo = null!;

    [OnReadyGet]
    private Button _proxyDemo = null!;

    public override void _Ready()
    {
        base._Ready();
        OnReady();
        _onReadyDemo.Pressed += () => GetTree().ChangeSceneToFile(Scenes.OnReadyDemoScreen.Path);
        _autoResDemo.Pressed += () => GetTree().ChangeSceneToFile(Scenes.AutoResDemoScreen.Path);
        _signalDemo.Pressed += () => GetTree().ChangeSceneToFile(Scenes.SignalDemoScreen.Path);
        _proxyDemo.Pressed += () => GetTree().ChangeSceneToFile(Scenes.ProxyDemoScreen.Path);
    }
}
