using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Serializers;

public class BencodeTokenConverter : JsonConverter<BencodeToken>
{
    public override BencodeToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, BencodeToken value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case BencodeInteger integer:
                writer.WriteNumberValue(integer.Value);
                break;

            case BencodeString str:
                writer.WriteStringValue(str.Value);
                break;
            
            // case BencodeList list:
            //     JsonSerializer.Serialize(writer, list.Values, options);
            //     break;
            //
            // case BencodeDictionary dict:
            //     JsonSerializer.Serialize(writer, dict.Values, options);
            //     break;

            default:
                throw new JsonException(
                    $"Unsupported token type: {value.GetType()}");
        }
    }
}