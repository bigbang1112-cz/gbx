using GBX.NET.Engines.Game;
using GBX.NET.Exceptions;

namespace BigBang1112.Gbx.Client;

public static class WorkflowFunctions
{
    public static IEnumerable<CGameCtnGhost> GetGhostsFromReplay(CGameCtnReplayRecord replay)
    {
        return replay.GetGhosts();
    }

    public static CGameCtnChallenge GetMapFromReplay(CGameCtnReplayRecord replay)
    {
        return replay.Challenge ?? throw new HeaderOnlyParseLimitationException();
    }

    public static byte[] GetMapDataFromReplay(CGameCtnReplayRecord replay)
    {
        return replay.ChallengeData ?? throw new HeaderOnlyParseLimitationException();
    }
}
