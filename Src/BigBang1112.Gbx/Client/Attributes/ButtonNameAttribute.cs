namespace BigBang1112.Gbx.Client.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ButtonNameAttribute : Attribute
{
    public string Name { get; }
    
    public ButtonNameAttribute(string name)
    {
        Name = name;
    }
}
