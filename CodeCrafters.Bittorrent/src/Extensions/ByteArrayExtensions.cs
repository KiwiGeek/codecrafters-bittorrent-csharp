namespace CodeCrafters.Bittorrent.Extensions;

public static class ByteArrayExtensions
{
    public static string ToHexString(this byte[] bytes) => Convert.ToHexStringLower(bytes);
    public static string ToUrlEncodedString(this byte[] bytes) => string.Concat(bytes.Select(b => $"%{b:X2}"));

    public static byte[][] ToChunks(this byte[] bytes, int chunkSize)
    {
        List<byte[]> results = [];
        int index = -chunkSize;
        while (index < bytes.Length)
        {
            results.Add(bytes.Skip(index+=chunkSize).Take(chunkSize).ToArray());
        }
        return results.ToArray();
    }
}