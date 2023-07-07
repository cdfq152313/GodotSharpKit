using Godot;

namespace GodotSharpKit;

public class SceneFactory<T> where T : Node
{
    public SceneFactory(PackedScene packedScene)
    {
        PackedScene = packedScene;
    }

    public PackedScene PackedScene;

    public T Instantiate()
    {
        return PackedScene.Instantiate<T>();
    }
}
