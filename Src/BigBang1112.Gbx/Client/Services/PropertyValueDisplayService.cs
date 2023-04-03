using BigBang1112.Gbx.Client.Attributes;
using BigBang1112.Gbx.Client.Components;
using BigBang1112.Gbx.Client.Components.PropertyValueDisplays;
using System.Reflection;

namespace BigBang1112.Gbx.Client.Services;

public interface IPropertyValueDisplayService
{
    bool TryGetComponent(Type type, out Type? typeComponent);
}

public class PropertyValueDisplayService : IPropertyValueDisplayService
{
    private readonly Dictionary<Type, Type> components;

    public PropertyValueDisplayService()
    {
        components = CachePropertyValueDisplays();
    }

    public bool TryGetComponent(Type type, out Type? typeComponent)
    {
        if (type.IsEnum)
        {
            typeComponent = typeof(EnumPropertyValueDisplay);
            return true;
        }

        return components.TryGetValue(type, out typeComponent);
    }

    private static Dictionary<Type, Type> CachePropertyValueDisplays()
    {
        var dict = new Dictionary<Type, Type>();

        foreach (var type in Assembly.GetExecutingAssembly().ExportedTypes)
        {
            if (!type.IsSubclassOf(typeof(PropertyValueDisplay)))
            {
                continue;
            }

            var att = type.GetCustomAttribute<AppliesForTypeAttribute>();

            if (att is null)
            {
                continue;
            }

            foreach (var attType in att.Types)
            {
                dict.Add(attType, type);
            }
        }

        return dict;
    }
}
