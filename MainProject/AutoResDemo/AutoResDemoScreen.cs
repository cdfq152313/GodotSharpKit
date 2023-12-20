using Godot;
using Godot4Demo.AutoResDemo.Deep.Deep2;
using GodotSharpKit.Misc;

namespace Godot4Demo.AutoResDemo;

public partial class AutoResDemoScreen : Node2D
{
    public override void _Ready()
    {
        // Resource and Path
        string imagePath = AutoRes.icon.Path;
        GD.Print(imagePath);
        Resource imageRes = AutoRes.icon.Load();

        // Typed Resource
        string fontPath = Fonts.MySystemFont.Path;
        GD.Print(fontPath);
        Font font = Fonts.MySystemFont.Load();

        // Special case: packed scene
        string deepNodePath = A.DeepNode.Path;
        GD.Print(deepNodePath);
        PackedScene deepNodePackedScene = A.DeepNode.Load();
        SceneFactory<DeepNode> deepNodeFactory = A.DeepNode.Factory;
        DeepNode deepNodeInstance = deepNodeFactory.Instantiate();
    }
}
