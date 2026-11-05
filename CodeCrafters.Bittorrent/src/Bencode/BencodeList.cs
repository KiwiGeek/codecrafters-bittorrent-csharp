using System.Reflection;
using System.Text.Json;
using CodeCrafters.Bittorrent.Enums;

namespace CodeCrafters.Bittorrent.Bencode;

public class BencodeList(List<BencodeToken> value) : BencodeToken<List<BencodeToken>>, IBencodeDecodable<BencodeList>
{
    
    private delegate BencodeToken DecodeDelegate(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode);

    private static readonly DecodeDelegate DecodeNested =
        (DecodeDelegate)typeof(BencodeDecoder)
            .GetMethod("Decode", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate(typeof(DecodeDelegate));
    
    public override List<BencodeToken> Value => value;
    
    public override byte[] ToBencode()
    {
        List<byte> output = [Convert.ToByte('l')];
        foreach (BencodeToken bt in Value)
        {
            byte[] newBytes = BencodeEncoder.Encode(bt);
            output.AddRange(newBytes);
        }
        output.Add(Convert.ToByte('e'));
        return output.ToArray();
    }

    public override string ToJson() => JsonSerializer.Serialize(Value);
    public static BencodeList Decode(ReadOnlySpan<byte> data, ref int offset, BencodeValidationMode mode = BencodeValidationMode.Strict)
    {
        offset++; // Consume 'l'
        List<BencodeToken> tokens = [];
        while (offset < data.Length && data[offset] != 101)
        {
            tokens.Add(DecodeNested(data, ref offset, mode));
        }
        if (offset >= data.Length) throw new FormatException("Invalid bencoded list.");
        offset++; // consume e
        return new BencodeList(tokens);
    }
}