using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Serializers;

namespace CodeCrafters.Bittorrent.Bencode;

[JsonConverter(typeof(BencodeTokenConverter))]
public abstract class BencodeToken;
