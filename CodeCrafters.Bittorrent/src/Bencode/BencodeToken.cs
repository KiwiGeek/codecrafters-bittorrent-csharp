using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Enums;
using CodeCrafters.Bittorrent.Serializers;

namespace CodeCrafters.Bittorrent.Bencode;

[JsonConverter(typeof(BencodeTokenConverter))]
public abstract class BencodeToken
{
    public abstract byte[] ToBencode();
    public abstract string ToJson();
}

public abstract class BencodeToken<T> : BencodeToken
{
    public abstract T Value { get; }
}

public interface IBencodeDecodable<TToken> where TToken : BencodeToken
{
    static abstract TToken Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode);
}