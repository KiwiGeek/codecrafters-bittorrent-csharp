using System.Text;

namespace CodeCrafters.Bittorrent.Bencode;

public static class BencodeDecoder
{
    public static BencodeToken Decode(ReadOnlySpan<byte> data)
    {
        return RecursiveDecode(data) ?? throw new InvalidOperationException();
    }

    private static BencodeToken? RecursiveDecode(ReadOnlySpan<byte> data)
    {
        // identify the type based on the first byte.
        byte firstByte = data[0];
        byte lastbyte = data[^1];
        switch (firstByte, lastbyte)
        {
            case (48, _):
            case (49, _):
            case (50, _):
            case (51, _):
            case (52, _):
            case (53, _):
            case (54, _):
            case (55, _):
            case (56, _):
            case (57, _):
                // it's a numeric digit; that makes this a BencodeString.
                return DecodeString(data);
                break;
            case (105, 101):
                // it's an i, and the last byte is an e; it's an integer
                return DecodeInteger(data);
            default:
                throw new NotImplementedException();
        }
    }

    private static BencodeString? DecodeString(ReadOnlySpan<byte> data)
    {
        // find the number at the start of the string; everything before a : (ascii 58)
        StringBuilder numberBuilder = new();
        int index = 0;
        while (data[index] != 58)
        {
            numberBuilder.Append(Convert.ToChar(data[index++]));
        }
        if (!int.TryParse(numberBuilder.ToString(), out int length)) throw new InvalidOperationException();
        BencodeString resultString = new BencodeString(data[(index+1)..].ToArray());
        return resultString;
    }

    private static BencodeInteger DecodeInteger(ReadOnlySpan<byte> data) =>
        new(long.Parse(Encoding.UTF8.GetString(data)[1..^1]));
}