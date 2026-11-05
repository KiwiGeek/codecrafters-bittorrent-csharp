using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Converter;

public class BencodeIntegerConverter : JsonConverter<BencodeInteger>
{
    public override BencodeInteger? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, BencodeInteger value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}