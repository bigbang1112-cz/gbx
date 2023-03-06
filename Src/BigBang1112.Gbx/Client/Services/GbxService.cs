using BigBang1112.Gbx.Client.Models;
using GBX.NET;
using System.Collections.ObjectModel;

namespace BigBang1112.Gbx.Client.Services;

public interface IGbxService
{
    ObservableCollection<GbxModel> Gbxs { get; }

    bool TryImport(Stream stream, out GbxModel? gbx);
}

public class GbxService : IGbxService
{
    public ObservableCollection<GbxModel> Gbxs { get; }

    public GbxService()
    {
        Gbxs = new ObservableCollection<GbxModel>();
    }

    public bool TryImport(Stream stream, out GbxModel? gbx)
    {
        try
        {
            gbx = new GbxModel(GameBox.Parse(stream));
            Gbxs.Add(gbx);
            return true;
        }
        catch (Exception)
        {
            gbx = null;
            return false;
        }
    }
}
