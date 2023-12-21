namespace GodotSharpKit.Misc;

[AttributeUsage(AttributeTargets.Interface)]
public class GodotProxy : Attribute
{
    public bool AutoSnakeCase;

    public GodotProxy(bool autoSnakeCase = true)
    {
        AutoSnakeCase = autoSnakeCase;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Delegate)]
public class GodotProxyName : Attribute
{
    public string Name;

    public GodotProxyName(string name)
    {
        Name = name;
    }
}
