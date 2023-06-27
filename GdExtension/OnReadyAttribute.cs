namespace GdExtension;

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyNode : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class OnReadyConnect : Attribute
{
    public OnReadyConnect(string signal, string? source = null)
    {
        Signal = signal;
        Source = source;
    }

    public string Signal;
    public string? Source;
}
