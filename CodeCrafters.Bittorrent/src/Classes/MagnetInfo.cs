using System.Net;
using System.Web;
using CodeCrafters.Bittorrent.Bencode;
using CodeCrafters.Bittorrent.Enums;
using CodeCrafters.Bittorrent.Extensions;

namespace CodeCrafters.Bittorrent.Classes;

public class MagnetInfo
{
    private static readonly HttpClient Http = new();
    public required string Announce { get; init; }
    public required byte[] InfoHash { get; init; }
    private Uri FullAnnounceUri => new($"{Announce}?peer_id=KiwiGeekTorrent12345&port=6881&uploaded=0&downloaded=0&left=1&compact=1&info_hash={InfoHash.ToUrlEncodedString()}");

    public static MagnetInfo FromMagentURL(string url)
    {
        Uri myUri = new Uri(url);
        string trackerUrl = HttpUtility.ParseQueryString(myUri.Query).Get("tr")!;
        string infoHash = HttpUtility.ParseQueryString(myUri.Query).Get("xt")![9..];
        return new MagnetInfo()
        {
            Announce = trackerUrl,
            InfoHash = Convert.FromHexString(infoHash)
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