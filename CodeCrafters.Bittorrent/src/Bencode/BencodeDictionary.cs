using System.Reflection;
using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.Comparer;
using CodeCrafters.Bittorrent.Enums;

namespace CodeCrafters.Bittorrent.Bencode;

public class BencodeDictionary(Dictionary<BencodeString, BencodeToken> values) : BencodeToken<Dictionary<BencodeString, BencodeToken>>, IBencodeDecodable<BencodeDictionary>
{
    private delegate BencodeToken DecodeDelegate(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode);

    private static readonly DecodeDelegate DecodeNested =
        (DecodeDelegate)typeof(BencodeDecoder)
            .GetMethod("Decode", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate(typeof(DecodeDelegate));
    
    public BencodeToken this[string key] => Value[new BencodeString(Encoding.UTF8.GetBytes(key))];
    
    public override Dictionary<BencodeString, BencodeToken> Value => values;

    public static BencodeDictionary Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        int startOffset = offset;
        if ((uint)startOffset >= (uint)data.Length || data[startOffset] != (byte)'d')
        {
            throw new FormatException("Invalid bencoded dictionary.");
        }

        int currentOffset = startOffset + 1;
        Dictionary<BencodeString, BencodeToken> tokens = new(new BencodeStringComparer());
        byte[]? previousKeyBytes = null;
        while (currentOffset < data.Length)
        {
            if (data[currentOffset] == (byte)'e')
            {
                offset = currentOffset + 1;
                return new BencodeDictionary(tokens);
            }

            BencodeString keyBytes = BencodeString.Decode(data, ref currentOffset);
            if (mode == BencodeValidationMode.Strict &&
                previousKeyBytes is not null &&
                keyBytes.Value.AsSpan().SequenceCompareTo(previousKeyBytes) <= 0)
            {
                throw new FormatException("Dictionary keys must be in strictly increasing byte order.");
            }
            
            BencodeToken value = DecodeNested(data, ref currentOffset, mode);
            tokens.Add(keyBytes, value);
            previousKeyBytes = keyBytes.Value;
        }

        throw new FormatException("Invalid bencoded dictionary.");
    }

    public override string ToString() => JsonSerializer.Serialize(Value);
    public override byte[] ToBencode()
    {
        List<byte> output = [Convert.ToByte('d')];
        List<(BencodeString key, BencodeToken value)> entries = Value.Select(pair => (pair.Key, pair.Value)).ToList();
        entries.Sort(static (left, right) => left.key.Value.AsSpan().SequenceCompareTo(right.key.Value));

        foreach ((BencodeString key, BencodeToken value) in entries)
        {
            output.AddRange(BencodeEncoder.Encode(key));
            output.AddRange(BencodeEncoder.Encode(value));
        }

        output.Add(Convert.ToByte('e'));
        return output.ToArray();
    }

    public override string ToJson() => JsonSerializer.Serialize(Value);
}