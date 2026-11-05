using CodeCrafters.Bittorrent.Enums;

namespace CodeCrafters.Bittorrent.Bencode;

public static class BencodeDecoder
{

    public static BencodeToken Decode(ReadOnlySpan<byte> data, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        int offset = 0;
        return Decode(data, ref offset, mode);
    }
    
    private static BencodeToken Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode)
    {
        if ((uint)offset >= (uint)data.Length) throw new FormatException("Unexpected end of Bencode data.");
        
        return data[offset] switch
        {
            (byte)'i' => BencodeInteger.Decode(data, ref offset, mode), 
            (byte)'l' => BencodeList.Decode(data, ref offset, mode), 
            (byte)'d' => BencodeDictionary.Decode(data, ref offset, mode),
            >= (byte)'0' and <= (byte)'9' => BencodeString.Decode(data, ref offset, mode),
            _ => throw new FormatException("Invalid Bencode token")
        };
    }

    public static bool TryDecode(ReadOnlySpan<byte> data, out BencodeToken? token, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        try
        {
            int offset = 0;
            token = Decode(data, ref offset, mode);
            return true;
        }
        catch
        {
            token = null;
            return false;
        }
    }
}