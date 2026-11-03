using System.Text.Json;

namespace CodeCrafters.Bittorrent.Bencode;

public class BencodeDictionary(Dictionary<string, BencodeToken> values) : BencodeToken
{
    public Dictionary<string, BencodeToken> Values => values;
    
    public override string ToString() => JsonSerializer.Serialize(Values);
}