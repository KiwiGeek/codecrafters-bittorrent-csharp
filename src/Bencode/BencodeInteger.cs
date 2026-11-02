using System.Text.RegularExpressions;

namespace CodeCrafters.Bittorrent.Bencode;

public sealed class BencodeInteger(long value) : BencodeToken
{
    public long Value { get; } = value;
}