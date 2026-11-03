using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
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
else if (command == "peers")
{
    byte[] torrentFileBytes = await File.ReadAllBytesAsync(param);
    BencodeToken token = BencodeDecoder.Decode(torrentFileBytes);
    TorrentFile infoTorrentFile = JsonSerializer.Deserialize<TorrentFile>(JsonSerializer.Serialize(token))!;
    BencodeDictionary infoDict = ((BencodeDictionary)token).Values.GetValueOrDefault("info") as BencodeDictionary ??
                                 throw new InvalidOperationException();
    UriBuilder builder = new UriBuilder(infoTorrentFile.Announce!);
    NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
    query["peer_id"] = "KiwiGeekTorrent12345";
    query["port"] = 6881.ToString();
    query["uploaded"] = 0.ToString();
    query["downloaded"] = 0.ToString();
    query["left"] = infoTorrentFile.Info!.Length.ToString();
    query["compact"] = 1.ToString();
    builder.Query = query.ToString();
    string finalUrl = builder.ToString();
    finalUrl = finalUrl + "&info_hash=" + string.Concat(SHA1.HashData(BencodeEncoder.Encode(infoDict)).Select(b => $"%{b:X2}"));
    HttpClient http = new();
    HttpRequestMessage request = new(HttpMethod.Get, finalUrl);
    HttpResponseMessage response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();
    byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
    BencodeDictionary responseDict = (BencodeDecoder.Decode(responseBody) as BencodeDictionary)!;
    BencodeString peers = responseDict.Values.GetValueOrDefault("peers") as BencodeString ?? throw new InvalidOperationException();
    int index = 0;
    while (index < peers.Value.Length)
    {
        byte[] ip = peers.Value[index..(index + 4)];
        byte[] port = peers.Value[(index+4)..(index+6)];
        System.Net.IPAddress ipAddress = new(ip);
        long portNumber = port[0] << 8 | port[1];
        index += 6;
        Console.WriteLine($"{ipAddress}:{portNumber}");
    }
}
else if (command == "handshake")
{
    // for the handshake, there's a third argument, in the format ipaddress:port
    string[] networkAddress = args[2].Split(":");
    IPEndPoint ipEndPoint = new(IPAddress.Parse(networkAddress[0]), int.Parse(networkAddress[1]));

    byte[] message = new byte[68];
    message[0] = 19;
    "BitTorrent protocol"u8.CopyTo(message.AsSpan(1));

    // we now need to get the infohash.
    byte[] torrentFileBytes = await File.ReadAllBytesAsync(param);
    BencodeToken token = BencodeDecoder.Decode(torrentFileBytes);
    BencodeDictionary infoDict = ((BencodeDictionary)token).Values.GetValueOrDefault("info") as BencodeDictionary ??
                                 throw new InvalidOperationException();
    byte[] infoHash = SHA1.HashData(BencodeEncoder.Encode(infoDict));
    infoHash.CopyTo(message, 28);

    byte[] peerId = RandomNumberGenerator.GetBytes(20);
    peerId.CopyTo(message, 48);



    using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    await client.ConnectAsync(ipEndPoint);
    _ = await client.SendAsync(message, SocketFlags.None);
    
    byte[] response = new byte[68];

    int received = await client.ReceiveAsync(response.AsMemory());
    
    byte[] responsePeerId = response.Skip(48).Take(20).ToArray();
    Console.WriteLine($"Peer ID: {Convert.ToHexStringLower(responsePeerId)}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
