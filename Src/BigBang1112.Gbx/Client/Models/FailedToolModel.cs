using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Models;

public record FailedToolModel(GbxModel Gbx, Exception Exception) : ITool;