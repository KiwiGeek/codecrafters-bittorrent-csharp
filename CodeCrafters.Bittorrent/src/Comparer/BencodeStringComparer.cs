using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Comparer;

public sealed class BencodeStringComparer : IEqualityComparer<BencodeString>
{
    public bool Equals(BencodeString? x, BencodeString? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Value.AsSpan().SequenceEqual(y.Value);
    }

    public int GetHashCode(BencodeString obj)
    {
        HashCode hash = new();

        foreach (byte b in obj.Value)
            hash.Add(b);

        return hash.ToHashCode();
    }
}