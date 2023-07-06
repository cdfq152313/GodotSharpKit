namespace GodotSharpKit;

[AttributeUsage(AttributeTargets.Class)]
public class OnReady : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyGet : Attribute
{
    public OnReadyGet(string? path = null)
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
public class OnReadyRun : Attribute
{
    public OnReadyRun(int order = 0)
    {
        Order = order;
    }

    public readonly int Order;
}

[AttributeUsage(AttributeTargets.Method)]
public class OnReadyLastRun : Attribute { }
