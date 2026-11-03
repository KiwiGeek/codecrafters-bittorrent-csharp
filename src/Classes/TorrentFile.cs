using System.Text.Json.Serialization;

namespace CodeCrafters.Bittorrent.Classes;

public class TorrentFile
{
    [JsonPropertyName("announce")]
    public string? Announce { get; init; }

    [JsonPropertyName("created by")]
    public string? CreatedBy { get; init; }

    [JsonPropertyName("info")]
    public TorrentFileInfo? Info { get; init; }
}