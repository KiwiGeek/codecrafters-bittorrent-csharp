using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using CodeCrafters.Bittorrent.Bencode;
using CodeCrafters.Bittorrent.Enums;
using CodeCrafters.Bittorrent.Extensions;

namespace CodeCrafters.Bittorrent.Classes;

public record TorrentInfo
{
    private static readonly HttpClient Http = new();
    public required string Announce { get; init; }
    public required string CreatedBy { get; init; }
    public required int Length { get; init; }
    public required string Name { get; init; }
    public required int PieceLength { get; init; }
    public required List<byte[]> Pieces { get; init; }
    private readonly BencodeDictionary _info;
    public byte[] InfoHash => SHA1.HashData(BencodeEncoder.Encode(_info));

    private Uri FullAnnounceUri => new($"{Announce}?peer_id=KiwiGeekTorrent12345&port=6881&uploaded=0&downloaded=0&left={Length}&compact=1&info_hash={InfoHash.ToUrlEncodedString()}");
    
    private TorrentInfo(BencodeDictionary infoDict)
    {
        _info = infoDict;
    }
    
    public static TorrentInfo FromTorrentFileBytes(byte[] fileBytes)
    {
        BencodeToken token = BencodeDecoder.Decode(fileBytes);
        TorrentFile infoTorrentFile = JsonSerializer.Deserialize<TorrentFile>(JsonSerializer.Serialize(token))!;
        ArgumentNullException.ThrowIfNull(infoTorrentFile.Announce);
        ArgumentNullException.ThrowIfNull(infoTorrentFile.Info);
        if (!infoTorrentFile.Info.Length.HasValue) throw new ArgumentNullException(nameof(infoTorrentFile.Info.Length));
        ArgumentNullException.ThrowIfNull(infoTorrentFile.Info.Name);
        if (!infoTorrentFile.Info.PieceLength.HasValue) throw new ArgumentNullException(nameof(infoTorrentFile.Info.PieceLength));

        BencodeDictionary infoDict = ((BencodeDictionary)token!).Value.GetValueOrDefault("info") as BencodeDictionary ?? throw new InvalidOperationException();
        BencodeString piecesBencodeString = infoDict.Value.GetValueOrDefault("pieces") as BencodeString ?? throw new InvalidOperationException();
        return new TorrentInfo(infoDict)
        {
            Announce = infoTorrentFile.Announce,
            CreatedBy = infoTorrentFile.CreatedBy ?? string.Empty,
            Length = infoTorrentFile.Info.Length.Value,
            Name = infoTorrentFile.Info.Name,
            PieceLength = infoTorrentFile.Info.PieceLength.Value,
            Pieces = piecesBencodeString.Value.Chunk(20).ToList()
        };
    }

    public static TorrentInfo FromMagnetBytes(byte[] infoBytes, string announce)
    {
        ArgumentNullException.ThrowIfNull(infoBytes);
        ArgumentNullException.ThrowIfNull(announce);
        BencodeDictionary infoDict = BencodeDecoder.Decode(infoBytes) as  BencodeDictionary ?? throw new InvalidOperationException();
        BencodeString piecesBencodeString = infoDict.Value.GetValueOrDefault("pieces") as BencodeString ?? throw new InvalidOperationException();
        return new TorrentInfo(infoDict)
        {
            Announce = announce,
            CreatedBy = string.Empty,
            Length = Convert.ToInt32(((BencodeInteger)infoDict["length"]).Value),
            Name = ((BencodeString)infoDict["name"]).ToString(),
            PieceLength = Convert.ToInt32(((BencodeInteger)infoDict["piece length"]).Value),
            Pieces = piecesBencodeString.Value.Chunk(20).ToList()
        };
    }

    public async Task<List<IPEndPoint>> GetPeersAsync()
    {
        List<IPEndPoint> result = [];
        HttpRequestMessage request = new(HttpMethod.Get, FullAnnounceUri);
        HttpResponseMessage response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
        BencodeDictionary responseDict = BencodeDecoder.Decode(responseBody, BencodeValidationMode.Lenient) as BencodeDictionary ?? throw new InvalidOperationException();
        BencodeString peers = responseDict.Value.GetValueOrDefault("peers") as BencodeString ?? throw new InvalidOperationException();
        List<byte[]> peerChunks = peers.Value.Chunk(6).ToList();
        result.AddRange(peerChunks.Select(peer => new IPEndPoint(new IPAddress(peer[..4]), peer[4] << 8 | peer[5])));
        return result;
    }
}