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
    
    bool HasOutput { get; }

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
    
    public bool HasOutput => ProduceMethods.Count > 0;

    public ToolFactory(ILogger<ToolFactory<T>> logger)
    {
        _logger = logger;

        ToolType = typeof(T);

        Id = ToolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        Name = ToolType.GetCustomAttribute<ToolNameAttribute>()?.Name ?? ToolType.Name;
        Description = ToolType.GetCustomAttribute<ToolDescriptionAttribute>()?.Description ?? string.Empty;
        GitHubRepository = ToolType.GetCustomAttribute<ToolGitHubAttribute>()?.Repository ?? string.Empty;
        Route = ToolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(Id);

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
                        ctorParamValuesToServe[i] = inputList.First();
                        break;
                    default:
                        if (bulkParamIndex is not null)
                        {
                            throw new Exception("Bulk input is supported with only one type of input.");
                        }

                        bulkParamIndex = i;
                        bulkParamList.AddRange(inputList);
                        break;
                }
            }

            if (invalidCtor)
            {
                continue;
            }

            if (bulkParamIndex is null)
            {
                yield return ctor.Invoke(ctorParamValuesToServe) as T ?? throw new Exception("Invalid constructor");
                continue;
            }

            foreach (var val in bulkParamList)
            {
                ctorParamValuesToServe[bulkParamIndex.Value] = val;
                yield return ctor.Invoke(ctorParamValuesToServe) as T ?? throw new Exception("Invalid constructor");
            }
        }
    }

    private static Dictionary<Type, ICollection<Node>> CreateInputObjectsDictionary(IEnumerable<GbxModel> gbxs)
    {
        var dict = new Dictionary<Type, ICollection<Node>>();

        foreach (var typeGroup in gbxs.Select(x => x.Object?.Node).OfType<Node>().GroupBy(obj => obj.GetType()))
        {
            var list = new List<Node>();

            foreach (var obj in typeGroup)
            {
                list.Add(obj);
            }

            dict.Add(typeGroup.Key, list);
        }

        return dict;
    }
}