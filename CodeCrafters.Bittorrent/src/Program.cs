using System.Diagnostics;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using CodeCrafters.Bittorrent.Bencode;
using CodeCrafters.Bittorrent.Classes;
using CodeCrafters.Bittorrent.Extensions;

namespace CodeCrafters.Bittorrent;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        // Parse arguments
        (string command, string parameter) = args.Length switch
        {
            0 or 1 => throw new InvalidOperationException("Usage: your_program.sh <command> <param>"),
            _ => (args[0].ToLower(), args[1].ToLower())
        };
        
        switch (command)
        {
            // Parse command and act accordingly
            case "decode":
            {
                if (!BencodeDecoder.TryDecode(Encoding.UTF8.GetBytes(parameter), out BencodeToken? token))
                    await Console.Error.WriteLineAsync($"Invalid encoded value: {parameter}");
                else
                    Console.WriteLine(token!.ToJson());
                break;
            }
            case "info":
            {
                TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(parameter));
                Console.WriteLine($"Tracker URL: {torrent.Announce}");
                Console.WriteLine($"Length: {torrent.Length}");
                Console.WriteLine($"Info Hash: {torrent.InfoHash.ToHexString()}");
                Console.WriteLine($"Piece Length: {torrent.PieceLength}");
                Console.WriteLine("Piece Hashes:");
                torrent.Pieces.ForEach(f=> Console.WriteLine(f.ToHexString()));
                break;
            }
            case "peers":
            {
                TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(parameter));
                List<IPEndPoint> peers = await torrent.GetPeersAsync();
                peers.ForEach(Console.WriteLine);
                break;
            }
            case "handshake":
            {
                IPEndPoint ipEndPoint = new(IPAddress.Parse(args[2].Split(":")[0]), int.Parse(args[2].Split(":")[1]));
                TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(parameter));
                Peer peer = new Peer(ipEndPoint);
                await peer.HandshakeAsync(torrent);
                Console.WriteLine($"Peer ID: {peer.Id!.ToHexString()}");
                peer.Dispose();
                break;
            }
            case "download_piece":
                {
                    string action = args[1];
                    Debug.Assert(action == "-o");
                    string pieceOutputFile = args[2];
                    string torrentFile = args[3];
                    long pieceIndex = Convert.ToInt64(args[4]);
                    TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(torrentFile));
                    List<IPEndPoint> peers = await torrent.GetPeersAsync();
                    Peer peer = new Peer(peers[Random.Shared.Next(0, peers.Count-1)]);
                    await peer.HandshakeAsync(torrent);
                    byte[] pieceData = await peer.GetPiece(pieceIndex);
                    await File.WriteAllBytesAsync(pieceOutputFile, pieceData);
                }
                break;
            case "download":
                {
                    string action = args[1];
                    Debug.Assert(action == "-o");
                    string torrentOutputFile = args[2];
                    string torrentFile = args[3];
                    TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(torrentFile));
                    List<IPEndPoint> peers = await torrent.GetPeersAsync();
                    Peer peer = new Peer(peers[Random.Shared.Next(0, peers.Count-1)]);
                    await peer.HandshakeAsync(torrent);
                    byte[] fileData = new byte[torrent.Length];
                    for (int i = 0; i < torrent.Pieces.Count; i++)
                    {
                        byte[] pieceData;
                        do
                        {
                            pieceData = await peer.GetPiece(i);
                        }
                        while (!SHA1.HashData(pieceData).AsSpan().SequenceEqual(torrent.Pieces[i]));
                        Console.WriteLine($"Received piece {i+1} of {torrent.Pieces.Count}");
                        pieceData.CopyTo(fileData, i*torrent.PieceLength);
                    }
                    await File.WriteAllBytesAsync(torrentOutputFile, fileData);
                }
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
        }
    }
}