namespace GodotSharpKit;

[AttributeUsage(AttributeTargets.Class)]
public class GdExtNode : Attribute { }

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

[AttributeUsage(AttributeTargets.Method)]
public class OnReady : Attribute
{
    public OnReady(int order = 0)
    {
        Order = order;
    }

    public readonly int Order;
}

[AttributeUsage(AttributeTargets.Method)]
public class OnReadyLast : Attribute { }
