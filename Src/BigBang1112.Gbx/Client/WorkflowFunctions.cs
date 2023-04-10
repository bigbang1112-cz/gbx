using BigBang1112.Gbx.Client.Attributes;
using GBX.NET.Engines.Game;
using GBX.NET.Exceptions;
using GbxToolAPI;

namespace BigBang1112.Gbx.Client;

public static class WorkflowFunctions
{
    [ButtonName("Extract ghosts")]
    public static IEnumerable<NodeFile<CGameCtnGhost>> GetGhostsFromReplay(CGameCtnReplayRecord replay)
    {
        return replay.GetGhosts().Select(x => new NodeFile<CGameCtnGhost>(x, Formatter.FormatAsFileName(x), GameVersion.IsManiaPlanet(x)));
    }

    [ButtonName("Extract map object")]
    public static NodeFile<CGameCtnChallenge> GetMapFromReplay(CGameCtnReplayRecord replay)
    {
        if (replay.Challenge is null)
        {
            throw new HeaderOnlyParseLimitationException();
        }

        return new NodeFile<CGameCtnChallenge>(replay.Challenge, Formatter.FormatAsFileName(replay.Challenge), GameVersion.IsManiaPlanet(replay.Challenge));
    }

    [ButtonName("Extract map binary")]
    public static byte[] GetMapDataFromReplay(CGameCtnReplayRecord replay)
    {
        return replay.ChallengeData ?? throw new HeaderOnlyParseLimitationException();
    }
}
