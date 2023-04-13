using BigBang1112.Gbx.Client.Attributes;
using GBX.NET.Engines.Game;
using GBX.NET.Exceptions;
using GbxToolAPI;
using TmEssentials;

namespace BigBang1112.Gbx.Client;

public static class WorkflowFunctions
{
    [ButtonName("Extract ghosts")]
    public static IEnumerable<NodeFile<CGameCtnGhost>> ExtractGhosts(CGameCtnReplayRecord replay)
    {
        return replay.GetGhosts().Select(x => new NodeFile<CGameCtnGhost>(x, Formatter.FormatAsFileName(x), GameVersion.IsManiaPlanet(x)));
    }
    
    [ButtonName("Extract ghosts")]
    public static IEnumerable<NodeFile<CGameCtnGhost>> ExtractGhosts(CGameCtnMediaClip clip)
    {
        return clip.GetGhosts().Select(x => new NodeFile<CGameCtnGhost>(x, Formatter.FormatAsFileName(x), GameVersion.IsManiaPlanet(x)));
    }

    [ButtonName("Extract map object")]
    public static NodeFile<CGameCtnChallenge> ExtractMapObject(CGameCtnReplayRecord replay)
    {
        if (replay.Challenge is null)
        {
            throw new HeaderOnlyParseLimitationException();
        }

        return new NodeFile<CGameCtnChallenge>(replay.Challenge, Formatter.FormatAsFileName(replay.Challenge), GameVersion.IsManiaPlanet(replay.Challenge));
    }

    [ButtonName("Extract map binary")]
    public static byte[] ExtractMapBinary(CGameCtnReplayRecord replay)
    {
        return replay.ChallengeData ?? throw new HeaderOnlyParseLimitationException();
    }

    [ButtonName("Extract embedded data")]
    public static BinFile? ExtractEmbeddedData(CGameCtnChallenge map)
    {
        using var ms = new MemoryStream();

        if (!map.ExtractOriginalEmbedZip(ms))
        {
            return null;
        }

        return new(Data: ms.ToArray(), FileName: $"{TextFormatter.Deformat(map.MapName)}-embed.zip");
    }
}
