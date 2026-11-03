using System.Text;

namespace CodeCrafters.Bittorrent.Bencode;

public static class BencodeDecoder
{
    public static BencodeToken Decode(ReadOnlySpan<byte> data)
    {
        int position = 0;
        return RecursiveDecode(data, ref position) ?? throw new InvalidOperationException();
    }

    private static BencodeToken? RecursiveDecode(ReadOnlySpan<byte> data, ref int position)
    {
        while (position < data.Length)
        {
            // identify the type based on the first byte.
            byte firstByte = data[position];
            switch (firstByte)
            {
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                    // it's a numeric digit; that makes this a BencodeString.
                    return DecodeString(data, ref position);
                case 100:
                    // it's a d; it's a dictionary
                    position++;
                    return DecodeDictionary(data, ref position);
                case 105:
                    // it's an i; it's an integer
                    return DecodeInteger(data, ref position);
                case 108:
                    position++;
                    return DecodeList(data, ref position);
                default:
                    throw new NotImplementedException();
            }   
        }
        return null;
    }

    private static BencodeString DecodeString(ReadOnlySpan<byte> data, ref int position)
    {
        // find the number at the start of the string; everything before a : (ascii 58)
        StringBuilder numberBuilder = new();
        int index = position;
        while (data[index] != 58) numberBuilder.Append(Convert.ToChar(data[index++]));
        if (!int.TryParse(numberBuilder.ToString(), out int _)) throw new InvalidOperationException();
        BencodeString resultString = new (data[(index+1)..(index+1 +int.Parse(numberBuilder.ToString()))].ToArray());
        position += numberBuilder.Length + resultString.Value.Length + 1;
        return resultString;
    }

    private static BencodeInteger DecodeInteger(ReadOnlySpan<byte> data, ref int position)
    {
        int endIndex = position;
        while (data[endIndex] != 101) endIndex++;
        string integerAsString = Encoding.UTF8.GetString(data)[(position+1)..(endIndex)];
        long integerAsLong = long.Parse(integerAsString);
        position = endIndex+1;
        return new BencodeInteger(integerAsLong);
    }

    private static BencodeList DecodeList(ReadOnlySpan<byte> data, ref int position)
    {
        List<BencodeToken> tokens = [];
        while (position < data.Length && data[position] != 101)
        {
            tokens.Add(RecursiveDecode(data, ref position)!);
        }
        position++; // consume e
        return new BencodeList(tokens);
    }

    private static BencodeDictionary DecodeDictionary(ReadOnlySpan<byte> data, ref int position)
    {
        Dictionary<string, BencodeToken> tokens = [];
        while (position < data.Length && data[position] != 101)
        {
            BencodeString keyBytes = DecodeString(data, ref position);
            string key = Encoding.UTF8.GetString(keyBytes.Value);
            BencodeToken value = RecursiveDecode(data, ref position)!;
            tokens.Add(key, value);
        }
        position++; // consume e
        return new  BencodeDictionary(tokens);
    }
        
}