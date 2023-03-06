using GBX.NET;

namespace BigBang1112.Gbx.Client.Models;

public class GbxModel
{
    public GameBox Object { get; }
    public Type Type { get; }

    public GbxModel(GameBox gbx)
    {
        Object = gbx ?? throw new ArgumentNullException(nameof(gbx));
        Type = gbx.Node?.GetType() ?? throw new Exception("Node cannot be null");
    }
}
