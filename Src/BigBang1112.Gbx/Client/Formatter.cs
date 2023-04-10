using GBX.NET.Engines.Game;
using TmEssentials;

namespace BigBang1112.Gbx.Client;

public static class Formatter
{
    public static string FormatAsFileName(CGameCtnGhost ghost)
    {
        var ghostName = ghost.GhostNickname is null ? ghost.GhostLogin : TextFormatter.Deformat(ghost.GhostNickname);

        return $"{ghostName}_{ghost.RaceTime.ToTmString(useApostrophe: true)}_{ghost.GhostUid}.Ghost.Gbx";
    }
    
    public static string FormatAsFileName(CGameCtnChallenge map)
    {
        return $"{TextFormatter.Deformat(map.MapName)}.{(GbxToolAPI.GameVersion.IsManiaPlanet(map) ? "Map" : "Challenge")}.Gbx";
    }
}
