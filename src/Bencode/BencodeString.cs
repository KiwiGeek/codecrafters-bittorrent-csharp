using System.Text;

namespace CodeCrafters.Bittorrent.Bencode;

public sealed class BencodeString(byte[] value) : BencodeToken
{
    public byte[] Value { get; } = value;

    public override string ToString() => Encoding.UTF8.GetString(Value);
}