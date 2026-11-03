using System.Text.Json.Serialization;

namespace CodeCrafters.Bittorrent.Classes;

public class TorrentFileInfo
{
    [JsonPropertyName("length")]
    public int? Length { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("piece length")]
    public int? PieceLength { get; init; }

    [JsonPropertyName("pieces")]
    public string? Pieces { get; init; }
}