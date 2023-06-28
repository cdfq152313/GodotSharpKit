namespace GdExtension;

[AttributeUsage(AttributeTargets.Class)]
public class OnReady : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyNode : Attribute
{
    public OnReadyNode(string? path = null)
    {
        Path = path;
    }

    public readonly string? Path;
}

[AttributeUsage(AttributeTargets.Method)]
public class OnReadyConnect : Attribute
{
    public OnReadyConnect(string source, string signal)
    {
        Source = source;
        Signal = signal;
    }

    public readonly string Signal;
    public readonly string Source;
}
