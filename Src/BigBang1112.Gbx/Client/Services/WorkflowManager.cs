using BigBang1112.Gbx.Client.Models;
using System.Net.Http.Json;

namespace BigBang1112.Gbx.Client.Services;

public interface IWorkflowManager
{
    IReadOnlyCollection<WorkflowModel> Workflows { get; }

    Task LoadWorkflowsAsync(CancellationToken cancellationToken = default);
    IEnumerable<WorkflowModel> GetWorkflows(string searchFilter = "");
}

public class WorkflowManager : IWorkflowManager
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public IReadOnlyCollection<WorkflowModel> Workflows { get; private set; } = Array.Empty<WorkflowModel>();

    public WorkflowManager(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task LoadWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var workflowList = await _http.GetFromJsonAsync<string[]>("workflow-list.json", cancellationToken) ?? Array.Empty<string>();

        var tasks = new List<Task<HttpResponseMessage>>();

        foreach (var workflow in workflowList)
        {
            tasks.Add(_http.GetAsync($"workflows/{workflow}.yml", cancellationToken));
        }

        await Task.WhenAll(tasks);

        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var workflows = new List<WorkflowModel>();

        foreach (var response in tasks.Select(x => x.Result))
        {
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            try
            {
                var workflow = deserializer.Deserialize<WorkflowModel>(reader);

                workflows.Add(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize workflow");
            }

            response.Dispose();
        }

        Workflows = workflows;
    }

    public IEnumerable<WorkflowModel> GetWorkflows(string searchFilter = "")
    {
        foreach (var workflow in Workflows)
        {
            if (FilterWorkflow(workflow, searchFilter))
            {
                yield return workflow;
            }
        }
    }

    private static bool FilterWorkflow(WorkflowModel workflow, string searchFilter)
    {
        if (workflow.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (workflow.Description?.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return false;
    }
}
