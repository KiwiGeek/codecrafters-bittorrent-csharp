using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.Bencode;
using CodeCrafters.Bittorrent.Classes;
using CodeCrafters.Bittorrent.Extensions;

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
else if (command == "encode")
{
    BencodeInteger integer = new (4);
    BencodeString str = new("hello"u8.ToArray());
    List<BencodeToken> listbt = [integer, str];
    BencodeList list = new(listbt);
    byte[] data = BencodeEncoder.Encode(list);
    Console.WriteLine(string.Join(" ", data.Select(b => b.ToString("X2"))));
}
else if (command == "info")
{
    byte[] torrentFileBytes = await File.ReadAllBytesAsync(param);
    BencodeToken token = BencodeDecoder.Decode(torrentFileBytes);
    TorrentFile infoTorrentFile = JsonSerializer.Deserialize<TorrentFile>(JsonSerializer.Serialize(token))!;
    Console.WriteLine($"Tracker URL: {infoTorrentFile.Announce}");
    Console.WriteLine($"Length: {infoTorrentFile.Info!.Length}");
    BencodeDictionary infoDict = ((BencodeDictionary)token).Values.GetValueOrDefault("info") as BencodeDictionary ??
                                 throw new InvalidOperationException();
    Console.WriteLine($"Info Hash: {Convert.ToHexStringLower(SHA1.HashData(BencodeEncoder.Encode(infoDict)))}");
    Console.WriteLine($"Piece Length: {infoTorrentFile.Info.PieceLength!}");
    Console.WriteLine("Piece Hashes:");
    BencodeString piecesBencodeString = infoDict.Values.GetValueOrDefault("pieces") as BencodeString ?? throw new InvalidOperationException();
    byte[] piecesByteArray = piecesBencodeString.Value;
    int index = 0;
    while (index < piecesByteArray.Length)
    {
        byte[] bytes = piecesByteArray.Skip(index).Take(20).ToArray();
        index += 20;
        Console.WriteLine(Convert.ToHexStringLower(bytes));
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
