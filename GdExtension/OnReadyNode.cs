namespace GdExtension;

[AttributeUsage(AttributeTargets.Field)]
public class OnReadyNode : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class OnReadyClass : Attribute
{
}