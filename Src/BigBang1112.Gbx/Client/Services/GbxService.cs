using BigBang1112.Gbx.Client.Models;
using GBX.NET;
using GbxToolAPI;
using System.Collections.ObjectModel;
using System.Text;

namespace BigBang1112.Gbx.Client.Services;

public interface IGbxService
{
    ObservableCollection<GbxModel> Gbxs { get; }

    bool TryImport(string fileName, Stream stream, out GbxModel? gbx);
}

public class GbxService : IGbxService
{
    public ObservableCollection<GbxModel> Gbxs { get; }

    public GbxService()
    {
        Gbxs = new ObservableCollection<GbxModel>();
    }

    public bool TryImport(string fileName, Stream stream, out GbxModel? gbx)
    {
        var isTextFile = IsTextFile(stream);

        stream.Position = 0;
        
        if (isTextFile)
        {
            using var r = new StreamReader(stream);
            gbx = new GbxModel(fileName, r.ReadToEnd());
            Gbxs.Add(gbx);
            return true;
        }

        try
        {
            gbx = new GbxModel(fileName, GameBox.Parse(stream));
            Gbxs.Add(gbx);
            return true;
        }
        catch (Exception)
        {
            gbx = null;
            return false;
        }
    }
    
    private static bool IsTextFile(Stream stream)
    {
        try
        {
            using var r = new StreamReader(stream, Encoding.UTF8, true, 1024, true);

            while (!r.EndOfStream)
            {
                int charValue = r.Read();
                if (charValue == 0)
                {
                    // file has null byte, considered binary
                    return false;
                }
            }

            // file doesn't contain null bytes or invalid UTF-8 sequences, considered text
            return true;
        }
        catch (DecoderFallbackException)
        {
            // invalid UTF-8 sequence, considered binary
            return false;
        }
    }
}
