using System.Text.Json;

namespace CodeCrafters.Bittorrent.Bencode;

public class BencodeList(List<BencodeToken> value) : BencodeToken
{
    public List<BencodeToken> Value => value;
    
    public override string ToString() => JsonSerializer.Serialize(Value);
}