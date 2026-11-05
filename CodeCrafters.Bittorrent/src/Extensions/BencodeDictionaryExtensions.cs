using System.Text;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Extensions;

public static class BencodeDictionaryExtensions
{
    public static BencodeToken? GetValueOrDefault(this Dictionary<BencodeString, BencodeToken> dictionary, string key)
    {
        return dictionary.GetValueOrDefault(new BencodeString(Encoding.UTF8.GetBytes(key)));
    }
}