using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.Bencode;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_program.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_program.sh <command> <param>"),
    _ => (args[0], args[1])
};

ReadOnlySpan<byte> paramByteArray = new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(param));

// Parse command and act accordingly
if (command == "decode")
{
    BencodeToken? token = BencodeDecoder.Decode(paramByteArray);
    if (token == null) throw new InvalidOperationException("Invalid encoded value: " + param);
    Console.WriteLine(JsonSerializer.Serialize(token));
    // var encodedValue = param;
    // if (Char.IsDigit(encodedValue[0]))
    // {
    //     // Example: "5:hello" -> "hello"
    //     var colonIndex = encodedValue.IndexOf(':');
    //     if (colonIndex != -1)
    //     {
    //         var strLength = int.Parse(encodedValue[..colonIndex]);
    //         var strValue = encodedValue.Substring(colonIndex + 1, strLength);
    //         Console.WriteLine(JsonSerializer.Serialize(strValue));
    //     }
    //     else
    //     {
    //         throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
    //     }
    // }
    // else if (BencodeToken.IsBencodeInteger(encodedValue))
    // {
    //     BencodeInteger bencodeInteger = BencodeInteger.FromString(encodedValue);
    //     Console.WriteLine(bencodeInteger.Value);
    // }
    // else
    // {
    //     throw new InvalidOperationException("Unhandled encoded value: " + encodedValue);
    // }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
