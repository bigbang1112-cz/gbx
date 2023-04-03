using BigBang1112.Gbx.Shared;
using GbxToolAPI;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace BigBang1112.Gbx.Server.Extensions;

internal static class ToolAssetsExtensions
{
    public static IApplicationBuilder UseToolAssets<T>(this IApplicationBuilder app)
    {
        var appDir = Path.GetDirectoryName(typeof(GbxServerApp).Assembly.Location) ?? "";

        var toolType = typeof(T);

        var assetsFolder = toolType.GetCustomAttribute<ToolAssetsAttribute>()?.Identifier ?? throw new Exception("Tool is missing ToolAssetsAttribute");
        var id = toolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        var route = toolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(id);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(appDir, "Assets", "Tools", assetsFolder)),
            RequestPath = $"/assets/tools/{route}",
            ServeUnknownFileTypes = true
        });

        return app;
    }
}
