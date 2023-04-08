using GbxToolAPI;
using GbxToolAPI.Server;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BigBang1112.Gbx.Server.Extensions;

internal static class ToolAssetsExtensions
{
    public static IEndpointRouteBuilder UseToolAssets<T>(this WebApplication app) where T : class, ITool, IHasAssets
    {
        var appDir = Path.GetDirectoryName(typeof(GbxServerApp).Assembly.Location) ?? "";

        var toolType = typeof(T);

        var assetsFolder = toolType.GetCustomAttribute<ToolAssetsAttribute>()?.Identifier ?? throw new Exception("Tool is missing ToolAssetsAttribute");
        var id = toolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        var route = toolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(id);

        var path = $"/assets/tools/{route}";

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(appDir, "Assets", "Tools", assetsFolder)),
            RequestPath = path,
            ServeUnknownFileTypes = true
        });

        app.Map(path, (IWebHostEnvironment env, IMemoryCache cache, string? game) =>
        {
            var dirPath = Path.Combine(Path.GetRelativePath(env.ContentRootPath, appDir), "Assets", "Tools", assetsFolder);

            if (!env.ContentRootFileProvider.GetDirectoryContents(dirPath).Any())
            {
                throw new Exception("Tool assets folder is empty");
            }

            game ??= "mp";

            if (game != "mp" && game != "tmf")
            {
                return Results.NotFound();
            }

            var key = $"ToolAssets:{id}:{game}";

            var data = cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var ms = new MemoryStream();

                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    AddAssetsToZip<T>(env.ContentRootFileProvider, zip, dirPath, dirPath, game != "tmf");
                }

                return ms.ToArray();
            }) ?? throw new Exception("Tool assets are null");
            
            return Results.File(data, "application/zip", $"{id}Assets.zip");
        });

        return app;
    }

    private static void AddAssetsToZip<T>(IFileProvider provider, ZipArchive archive, string parentPath, string stripOffPath, bool isManiaPlanet) where T : class, ITool, IHasAssets
    {
        foreach (var file in provider.GetDirectoryContents(parentPath))
        {
            var filePath = Path.Combine(parentPath, file.Name);
            
            if (file.IsDirectory) // Recursively call the function for each subdirectory
            {
                AddAssetsToZip<T>(provider, archive, filePath, stripOffPath, isManiaPlanet);
                continue;
            }

            if ((isManiaPlanet && filePath.EndsWith(".png")) || (!isManiaPlanet && filePath.EndsWith(".webp")))
            {
                continue;
            }
            
            // Add the file to the zip archive
            
            var path = Path.GetRelativePath(stripOffPath, filePath);

            var remappedPath = T.RemapAssetRoute(path, isManiaPlanet);

            if (string.IsNullOrEmpty(remappedPath))
            {
                continue;
            }

            using var stream = archive.CreateEntry(remappedPath).Open();
            using var contentStream = file.CreateReadStream();

            contentStream.CopyTo(stream);
        }
    }
}
