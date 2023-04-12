using GBX.NET;
using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Models;

public class GbxModel
{
    public string FileName { get; }
    public GameBox? Object { get; }
    public Type? Type { get; }
    public string? Text { get; }
    public byte[]? Data { get; }

    public GbxModel(string fileName, GameBox gbx)
    {
        FileName = fileName;
        Object = gbx ?? throw new ArgumentNullException(nameof(gbx));
        Type = gbx.Node?.GetType() ?? throw new Exception("Node cannot be null");
    }

    public GbxModel(string fileName, string text)
    {
        FileName = fileName;
        Text = text;
    }

    public GbxModel(string fileName, byte[] data)
    {
        FileName = fileName;
        Data = data;
    }
}
