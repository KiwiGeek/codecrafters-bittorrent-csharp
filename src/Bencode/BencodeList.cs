using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Serializers;

namespace CodeCrafters.Bittorrent.Bencode;


public class BencodeList(List<BencodeToken> value) : BencodeToken
{
    public List<BencodeToken> Value => value;
    
    public override string ToString() => JsonSerializer.Serialize(Value);
}