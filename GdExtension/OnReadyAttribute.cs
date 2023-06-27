namespace GdExtension;

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyNode : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class OnReadyConnect : Attribute
{
    public OnReadyConnect(string signal, string node = null)
    {
        Signal = signal;
        Node = node;
    }

    public string Signal;
    public string Node;
}
