using Godot;

namespace GdExtension;

public class TypedPackedScene<T> where T : Node
{
    public TypedPackedScene(PackedScene packedScene)
    {
        PackedScene = packedScene;
    }

    public PackedScene PackedScene;

    public T Instantiate()
    {
        return PackedScene.Instantiate<T>();
    }
}
