namespace GodotSharpKit.Misc;

[AttributeUsage(AttributeTargets.Interface)]
public class GodotProxy : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Delegate)]
public class GodotProxyName : Attribute
{
    public string Name;

    public GodotProxyName(string name)
    {
        Name = name;
    }
}
