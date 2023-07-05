using Godot;

namespace GdExtension;

public class Res<T> where T : class
{
    public Res(string path)
    {
        Path = path;
    }

    public readonly string Path;

    public T Load() => GD.Load<T>(Path);
}

public class SceneRes<T> : Res<PackedScene> where T : Node
{
    public SceneRes(string path) : base(path) { }

    public TypedPackedScene<T> Pack()
    {
        return new TypedPackedScene<T>(Load());
    }
}
