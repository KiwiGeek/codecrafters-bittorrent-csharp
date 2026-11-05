using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Converter;

public class BencodeStringConverter : JsonConverter<BencodeString> 
{
    public override BencodeString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, BencodeString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Encoding.UTF8.GetString(value.Value));
    }
    
    public override void WriteAsPropertyName(
        Utf8JsonWriter writer, BencodeString value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(Encoding.UTF8.GetString(value.Value));
    }
}