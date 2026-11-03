using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.Bencode;
using CodeCrafters.Bittorrent.Classes;

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
}
else if (command == "info")
{
    string fileName = param;
    byte[] torrentFileBytes = await File.ReadAllBytesAsync(fileName);
    BencodeToken? token = BencodeDecoder.Decode(torrentFileBytes);
    TorrentFile infoTorrentFile = JsonSerializer.Deserialize<TorrentFile>(JsonSerializer.Serialize(token))!;
    Console.WriteLine($"Tracker URL: {infoTorrentFile.Announce}");
    Console.WriteLine($"Length: {infoTorrentFile.Info!.Length}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
