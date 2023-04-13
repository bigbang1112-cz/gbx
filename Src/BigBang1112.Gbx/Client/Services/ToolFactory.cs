using BigBang1112.Gbx.Client.Models;
using BigBang1112.Gbx.Shared;
using GBX.NET;
using GbxToolAPI;
using System.Reflection;

namespace BigBang1112.Gbx.Client.Services;

public interface IToolFactory
{
    Type ToolType { get; }

    string Id { get; }
    string Name { get; }
    string Description { get; }
    string GitHubRepository { get; }
    string Route { get; }
    IReadOnlyCollection<MethodInfo> ProduceMethods { get; }
    
    Type? ConfigType { get; }
    Type? ComponentType { get; }
    Type? ComponentProceedType { get; }

    bool HasOutput { get; }
    bool HasAssets { get; }

    ITool CreateTool(params object?[] args);
    IEnumerable<ITool> CreateTools(IEnumerable<GbxModel> gbxs);
}

public class ToolFactory<T> : IToolFactory where T : class, ITool
{
    private readonly ILogger<ToolFactory<T>> _logger;

    public Type ToolType { get; }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string GitHubRepository { get; }
    public string Route { get; }
    public IReadOnlyCollection<MethodInfo> ProduceMethods { get; }
    
    public Type? ConfigType { get; }
    public Type? ComponentType { get; }
    public Type? ComponentProceedType { get; }

    public bool HasOutput => ProduceMethods.Count > 0;
    public bool HasAssets { get; }

    internal ToolFactory(ILogger<ToolFactory<T>> logger, Type? componentType, Type? componentProceedType)
    {
        _logger = logger;

        ToolType = typeof(T);

        Id = ToolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        Name = ToolType.GetCustomAttribute<ToolNameAttribute>()?.Name ?? ToolType.Name;
        Description = ToolType.GetCustomAttribute<ToolDescriptionAttribute>()?.Description ?? string.Empty;
        GitHubRepository = ToolType.GetCustomAttribute<ToolGitHubAttribute>()?.Repository ?? string.Empty;
        Route = ToolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(Id);

        foreach(var iface in ToolType.GetInterfaces())
        {
            if (iface.IsGenericType)
            {
                var def = iface.GetGenericTypeDefinition();

                if (def == typeof(IConfigurable<>))
                {
                    ConfigType = iface.GetGenericArguments()[0];
                }
                
                continue;
            }

            if (iface == typeof(IHasAssets))
            {
                HasAssets = true;
            }
        }

        var produceMethods = new List<MethodInfo>();
        ProduceMethods = produceMethods;

        foreach (var iface in ToolType.GetInterfaces())
        {
            if (iface.IsGenericType)
            {
                var genericDef = iface.GetGenericTypeDefinition();

                if (genericDef == typeof(IHasOutput<>))
                {
                    produceMethods.Add(iface.GetMethod("Produce")!);
                }
            }
        }

        ComponentType = componentType;
        ComponentProceedType = componentProceedType;
    }

    ITool IToolFactory.CreateTool(params object?[] args)
    {
        return CreateTool(args);
    }

    public T CreateTool(params object?[] args)
    {
        _logger.LogInformation("Creating tool {ToolType} with args {Args}", typeof(T), args);

        return (T)Activator.CreateInstance(typeof(T), args)!;
    }

    IEnumerable<ITool> IToolFactory.CreateTools(IEnumerable<GbxModel> gbxs)
    {
        foreach (var tool in CreateTools(gbxs))
        {
            yield return tool;
        }
    }

    public IEnumerable<T> CreateTools(IEnumerable<GbxModel> gbxs)
    {
        var inputByType = CreateInputObjectsDictionary(gbxs);

        foreach (var ctor in typeof(T).GetConstructors())
        {
            var ctorParams = ctor.GetParameters();

            var invalidCtor = false;
            var bulkParamIndex = default(int?);
            var ctorParamValuesToServe = new object[ctorParams.Length];
            var bulkParamList = new List<object>();

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var param = ctorParams[i];

                if (!inputByType.TryGetValue(param.ParameterType, out var inputList))
                {
                    invalidCtor = true;
                    break;
                }

                switch (inputList.Count)
                {
                    case <= 0:
                        throw new Exception("No input for parameter " + param.Name + " of type " + param.ParameterType.Name);
                    case 1:
                        if (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(param.ParameterType.GetGenericArguments()[0]))!;

                            foreach (var item in inputList.Select(x => ObjectFromGbxModel(x, param)))
                            {
                                list.Add(item);
                            }
                            
                            ctorParamValuesToServe[i] = list;
                        }
                        else
                        {
                            ctorParamValuesToServe[i] = inputList.Select(x => ObjectFromGbxModel(x, param)).First();
                        }
                        break;
                    default:
                        if (bulkParamIndex is not null)
                        {
                            throw new Exception("Bulk input is supported with only one type of input.");
                        }

                        bulkParamIndex = i;
                        bulkParamList.AddRange(inputList.Select(x => ObjectFromGbxModel(x, param)));
                        break;
                }
            }

            if (invalidCtor)
            {
                continue;
            }

            if (bulkParamIndex is null)
            {
                T toolInstance;

                try
                {
                    toolInstance = ctor.Invoke(ctorParamValuesToServe) as T ?? throw new Exception("Invalid constructor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create tool {ToolType}", typeof(T));
                    continue;
                }

                yield return toolInstance;
                break;
            }

            var paramType = ctorParams[bulkParamIndex.Value].ParameterType;

            foreach (var givenVal in bulkParamList)
            {
                var val = givenVal;
                var isList = paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (isList)
                {
                    var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(paramType.GetGenericArguments()[0]))!;
                    list.Add(givenVal);
                    val = list;
                }

                ctorParamValuesToServe[bulkParamIndex.Value] = val;

                T toolInstance;

                try
                {
                    toolInstance = ctor.Invoke(ctorParamValuesToServe) as T ?? throw new Exception("Invalid constructor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create tool {ToolType}", typeof(T));
                    continue;
                }

                yield return toolInstance;

                if (isList)
                {
                    yield break;
                }
            }
        }
    }

    private static Dictionary<Type, ICollection<GbxModel>> CreateInputObjectsDictionary(IEnumerable<GbxModel> gbxs)
    {
        var dict = new Dictionary<Type, ICollection<GbxModel>>();

        foreach (var typeGroup in gbxs.GroupBy<GbxModel, Type>(TypeOfGbxGrouping))
        {
            var list = typeGroup.ToList();

            dict.Add(typeGroup.Key, list);
            dict.Add(typeof(IEnumerable<>).MakeGenericType(typeGroup.Key), list);
        }

        return dict;
    }

    private static Type TypeOfGbxGrouping(GbxModel gbx)
    {
        if (gbx.Type is not null)
        {
            return gbx.Type;
        }
        
        if (gbx.Text is not null)
        {
            return typeof(TextFile);
        }

        throw new NotSupportedException("Unknown GbxModel type");
    }

    private static object ObjectFromGbxModel(GbxModel gbx, ParameterInfo param)
    {
        if (gbx.Object?.Node is not null)
        {
            return gbx.Object.Node;
        }

        if (gbx.Text is not null)
        {
            if (param.ParameterType == typeof(TextFile))
            {
                return new TextFile(gbx.Text, gbx.FileName);
            }
            else if(param.ParameterType == typeof(string))
            {
                return gbx.Text;
            }
            else
            {
                throw new NotSupportedException("Text is not supported for parameter type " + param.ParameterType.Name);
            }
        }

        throw new NotSupportedException("Not supported for parameter type " + param.ParameterType.Name);
    }
}