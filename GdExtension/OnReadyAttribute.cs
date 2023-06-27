namespace GdExtension;

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyNode : Attribute { }

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
