using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Converter;
using CodeCrafters.Bittorrent.Enums;

namespace CodeCrafters.Bittorrent.Bencode;

[JsonConverter(typeof(BencodeIntegerConverter))]
public sealed class BencodeInteger(long value) : BencodeToken<long>, IBencodeDecodable<BencodeInteger>
{
    public override long Value { get; } = value;
    public override byte[] ToBencode()
    {
        List<byte> output = [Convert.ToByte('i')];
        foreach (char c in Value.ToString(CultureInfo.InvariantCulture))
        {
            output.Add(Convert.ToByte(c));
        }
        output.Add(Convert.ToByte('e'));
        return output.ToArray();
    }

    public override string ToJson() => JsonSerializer.Serialize(this);

    public static BencodeInteger Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        int startIndex = offset;
        if (startIndex < 0 || startIndex >= data.Length || data[startIndex] != (byte)'i')
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        int endIndex = startIndex + 1;
        while (endIndex < data.Length && data[endIndex] != (byte)'e')
        {
            endIndex++;
        }

        if (endIndex == data.Length)
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        ReadOnlySpan<byte> integerBytes = data[(startIndex + 1)..endIndex];
        if (integerBytes.IsEmpty)
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        int digitsStart = integerBytes[0] == (byte)'-' ? 1 : 0;
        if (digitsStart == integerBytes.Length)
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        ReadOnlySpan<byte> digits = integerBytes[digitsStart..];
        foreach (byte digit in digits)
        {
            if (digit < (byte)'0' || digit > (byte)'9')
            {
                throw new FormatException("Invalid bencoded integer.");
            }
        }

        if (digits[0] == (byte)'0' && (digits.Length > 1 || digitsStart == 1))
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        string integerAsString = Encoding.ASCII.GetString(integerBytes);
        if (!long.TryParse(
                integerAsString,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out long integerValue))
        {
            throw new FormatException("Invalid bencoded integer.");
        }

        offset = endIndex + 1;
        return new BencodeInteger(integerValue);
    }
}