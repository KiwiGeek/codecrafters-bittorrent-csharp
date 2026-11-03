using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Serializers;

public class BencodeListConverter : JsonConverter<BencodeList>
{
    public override BencodeList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, BencodeList value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (BencodeToken bt in value.Value)
        {
            JsonSerializer.Serialize(writer, bt, options);
            break;
        }
        writer.WriteEndArray();
    }
}