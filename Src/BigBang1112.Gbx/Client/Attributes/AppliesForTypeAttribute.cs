namespace BigBang1112.Gbx.Client.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AppliesForTypeAttribute : Attribute
{
    public Type[] Types { get; }
    
    public AppliesForTypeAttribute(params Type[] types)
    {
        Types = types;
    }
}
