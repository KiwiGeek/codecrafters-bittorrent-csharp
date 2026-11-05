using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Converter;
using CodeCrafters.Bittorrent.Enums;

namespace CodeCrafters.Bittorrent.Bencode;

[JsonConverter(typeof(BencodeStringConverter))]
public sealed class BencodeString(byte[] value) : BencodeToken<byte[]>, IBencodeDecodable<BencodeString>
{
    public override byte[] Value { get; } = value;
    
    public override byte[] ToBencode()
    {
        List<byte> output = [];
        string strLength = Value.Length.ToString(CultureInfo.InvariantCulture);
        foreach (char c in strLength)
        {
            output.Add(Convert.ToByte(c));
        } 
        output.Add(Convert.ToByte(':'));
        foreach (char c in Value)
        {
            output.Add(Convert.ToByte(c));
        }
        return output.ToArray();
    }

    public override string ToJson() => JsonSerializer.Serialize(this);
    public static BencodeString Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        if (offset < 0 || offset >= data.Length || data[offset] < (byte)'0' || data[offset] > (byte)'9')
        {
            throw new FormatException("Invalid bencoded string.");
        }

        int index = offset;
        int length = 0;
        bool leadingZero = data[index] == (byte)'0';
        while (index < data.Length && data[index] != (byte)':')
        {
            byte digit = data[index];
            if (digit < (byte)'0' || digit > (byte)'9' || (leadingZero && index > offset))
            {
                throw new FormatException("Invalid bencoded string.");
            }

            int digitValue = digit - (byte)'0';
            if (length > (int.MaxValue - digitValue) / 10)
            {
                throw new FormatException("Invalid bencoded string.");
            }

            length = length * 10 + digitValue;
            index++;
        }

        if (index == data.Length || length > data.Length - index - 1)
        {
            throw new FormatException("Invalid bencoded string.");
        }

        int payloadStart = index + 1;
        BencodeString resultString = new(data.Slice(payloadStart, length).ToArray());
        offset = payloadStart + length;
        return resultString;
    }
}