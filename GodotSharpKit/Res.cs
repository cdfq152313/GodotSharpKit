using Godot;

namespace GodotSharpKit;

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

    public SceneFactory<T> Factory => new(Load());
}
