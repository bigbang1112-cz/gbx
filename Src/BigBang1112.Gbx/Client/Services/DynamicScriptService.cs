using GbxToolAPI.Client.Models;
using GbxToolAPI.Client.Services;
using Microsoft.JSInterop;

namespace BigBang1112.Gbx.Client.Services;

public class DynamicScriptService : IDynamicScriptService
{
    private readonly IJSRuntime _js;

    public DynamicScriptService(IJSRuntime js)
    {
        _js = js;
    }
    
    public async Task SpawnScriptAsync(string src, string id)
    {
        await _js.InvokeVoidAsync("spawn_script", src, id);
    }

    public async Task SpawnScriptAsync(Script script)
    {
        await _js.InvokeVoidAsync("spawn_script", script);
    }

    public async Task SpawnScriptsAsync(params Script[] scripts)
    {
        await _js.InvokeVoidAsync("spawn_scripts", new[] { scripts });
    }
    
    public async Task DespawnScriptAsync(string id)
    {
        await _js.InvokeVoidAsync("despawn_script", id);
    }
}
