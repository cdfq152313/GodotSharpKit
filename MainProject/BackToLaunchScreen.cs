using Godot;
using GodotSharpKit.Misc;

namespace Godot4Demo;

[OnReady]
public partial class BackToLaunchScreen : CanvasLayer
{
    [OnReadyGet]
    private Button _button = null!;

    public override void _Ready()
    {
        base._Ready();
        OnReady();
        _button.Pressed += () => GetTree().ChangeSceneToFile(Scenes.LaunchScreen.Path);
    }
}
