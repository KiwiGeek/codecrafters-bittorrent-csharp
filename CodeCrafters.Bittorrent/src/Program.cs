using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Web;
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
                string torrentOutputFile = args[2];
                string torrentFile = args[3];
                TorrentInfo torrent = TorrentInfo.FromTorrentFileBytes(await File.ReadAllBytesAsync(torrentFile));
                List<IPEndPoint> endpoints = await torrent.GetPeersAsync();
                byte[] fileData = new byte[torrent.Length];
                ConcurrentQueue<int> pendingPieces = new(Enumerable.Range(0, torrent.Pieces.Count));
                await Parallel.ForEachAsync(endpoints.Take(5), new ParallelOptions { MaxDegreeOfParallelism = 5 },
                    async (endpoint, cancellationToken) =>
                    {
                        using Peer peer = new(endpoint);
                        await peer.HandshakeAsync(torrent);
                        while (pendingPieces.TryDequeue(out int i))
                        {
                            byte[] pieceData;
                            do
                            {
                                pieceData = await peer.GetPiece(i);
                            }
                            while (!SHA1.HashData(pieceData).AsSpan().SequenceEqual(torrent.Pieces[i]));
                            pieceData.CopyTo(fileData, i * torrent.PieceLength);
                            Console.WriteLine($"Received piece {i + 1} of {torrent.Pieces.Count}");
                        }
                    });
                await File.WriteAllBytesAsync(torrentOutputFile, fileData);
            }
                break;
            case "magnet_parse":
            {
                MagnetInfo magnet = MagnetInfo.FromMagentURL(parameter);
                Console.WriteLine($"Tracker URL: {magnet.Announce}");
                Console.WriteLine($"Info Hash: {magnet.InfoHash.ToHexString()}");
            }
                break;
            case "magnet_handshake":
            {
                MagnetInfo magnet = MagnetInfo.FromMagentURL(parameter);
                List<IPEndPoint> peers = (await magnet.GetPeersAsync())
                    .Where(p => p.Port != 6881)
                    .ToList();
                Peer peer = new (peers[Random.Shared.Next(0, peers.Count)]);
                await peer.HandshakeAsync(magnet);
                Console.WriteLine($"Peer ID: {peer.Id!.ToHexString()}");
                long peerMetadataId = await peer.ExtensionHandshakeAsync(magnet);
                Console.WriteLine($"Peer Metadata Extension ID: {peerMetadataId}");
                peer.Dispose();
            }
                break;
            case "magnet_info":
            {
                MagnetInfo magnet = MagnetInfo.FromMagentURL(parameter);
                List<IPEndPoint> peers = (await magnet.GetPeersAsync())
                    .Where(p => p.Port != 6881)
                    .ToList();
                Peer peer = new (peers[Random.Shared.Next(0, peers.Count)]);
                await peer.HandshakeAsync(magnet);
                await peer.ExtensionHandshakeAsync(magnet);
                TorrentInfo torrent = await peer.RequestMetadata(magnet);
                Console.WriteLine($"Tracker URL: {torrent.Announce}");
                Console.WriteLine($"Length: {torrent.Length}");
                Console.WriteLine($"Info Hash: {torrent.InfoHash.ToHexString()}");
                Console.WriteLine($"Piece Length: {torrent.PieceLength}");
                Console.WriteLine("Piece Hashes:");
                torrent.Pieces.ForEach(f=> Console.WriteLine(f.ToHexString()));
            }
                break;
            case "magnet_download_piece":
            {
                string action = args[1];
                Debug.Assert(action == "-o");
                string pieceOutputFile = args[2];
                string magnetLink = args[3];
                long pieceIndex = Convert.ToInt64(args[4]);
                Console.WriteLine($"aciton: {action}");
                Console.WriteLine($"output file: {pieceOutputFile}");
                Console.WriteLine($"Magnet Link: {magnetLink}");
                Console.WriteLine($"Piece Index: {pieceIndex}");
                MagnetInfo magnet = MagnetInfo.FromMagentURL(magnetLink);
                List<IPEndPoint> peers = (await magnet.GetPeersAsync())
                    .Where(p => p.Port != 6881)
                    .ToList();
                Console.WriteLine("Peers: " + peers.Count);
                Peer peer = new (peers[Random.Shared.Next(0, peers.Count)]);
                await peer.HandshakeAsync(magnet);
                Console.WriteLine("Handshaked");
                await peer.ExtensionHandshakeAsync(magnet);
                Console.WriteLine("Extended handshake");
                TorrentInfo torrent = await peer.RequestMetadata(magnet);
                Console.WriteLine("Got Torrent");
                byte[] pieceData = await peer.GetPiece(pieceIndex);
                await File.WriteAllBytesAsync(pieceOutputFile, pieceData);
            }
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
        }
    }
}