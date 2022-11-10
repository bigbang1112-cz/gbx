namespace BigBang1112.Gbx.Server.Models;

public record FileRef(byte Version, byte[]? Checksum, string FilePath, string? LocatorUrl);