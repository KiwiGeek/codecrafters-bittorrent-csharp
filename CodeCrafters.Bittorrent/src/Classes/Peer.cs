using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Classes;

public class Peer(IPEndPoint ipEndPoint) : IDisposable
{
    public byte[]? Id { get; private set; }
    private Socket? _client;
    private readonly byte[] _buffer = new byte[4096];
    private TorrentInfo _torrent;
    private readonly TaskCompletionSource _unchoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _bitfieldReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<byte[]> _blockReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _loopStarted = false;
    private bool _supportsExtensions = false;
    private long _peerMetadataExtensionId = 0;

    public async Task HandshakeAsync(TorrentInfo torrent)
    {
        byte[] message = new byte[68];
        message[0] = 19;
        "BitTorrent protocol"u8.CopyTo(message.AsSpan(1));
        torrent.InfoHash.CopyTo(message, 28);
        RandomNumberGenerator.GetBytes(20).CopyTo(message, 48);
        _client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await _client.ConnectAsync(ipEndPoint);
        await _client.SendAsync(message, SocketFlags.None);
        byte[] response = new byte[68];
        await _client.ReceiveAsync(response.AsMemory());
        Id = new byte[20];
        response.Skip(48).Take(20).ToArray().CopyTo(Id, 0);
        _torrent = torrent;
    }

    public async Task HandshakeAsync(MagnetInfo magent)
    {
        byte[] message = new byte[68];
        message[0] = 19;
        "BitTorrent protocol"u8.CopyTo(message.AsSpan(1));
        message[25] = 16;
        magent.InfoHash.CopyTo(message, 28);
        RandomNumberGenerator.GetBytes(20).CopyTo(message, 48);
        _client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await _client.ConnectAsync(ipEndPoint);
        await _client.SendAsync(message, SocketFlags.None);
        byte[] response = new byte[68];
        await _client.ReceiveAsync(response.AsMemory());
        if (response[25] == 16) _supportsExtensions = true;
        Id = new byte[20];
        response.Skip(48).Take(20).ToArray().CopyTo(Id, 0);
    }
    
    public async Task<long> ExtensionHandshakeAsync(MagnetInfo magent)
    {
        // await the bitfield message
        byte[] bitfieldResponse = new byte[6];
        await _client.ReceiveAsync(bitfieldResponse, SocketFlags.None);
        BencodeToken extensionDictionary = BencodeDecoder.Decode("d1:md11:ut_metadatai69eee"u8);
        byte[] extensionMessagePayload = [0, .. extensionDictionary.ToBencode()];
        byte[] message = new byte[extensionMessagePayload.Length + 5];
        extensionMessagePayload.CopyTo(message, 5);
        message[4] = 20;
        message[3] = Convert.ToByte(extensionMessagePayload.Length+1);
        await _client.SendAsync(message, SocketFlags.None);
        byte[] extensionResponse = new byte[4];
        await _client.ReceiveAsync(extensionResponse, SocketFlags.None);
        byte[] responseBytes = new byte[Convert.ToInt32(extensionResponse[3])];
        await _client.ReceiveAsync(responseBytes.AsMemory());
        _peerMetadataExtensionId = ((BencodeInteger)((BencodeDictionary)(BencodeDecoder.Decode(responseBytes[2..]) as BencodeDictionary)!["m"])["ut_metadata"]).Value;
        return _peerMetadataExtensionId;
    }
    
    public async Task<TorrentInfo> RequestMetadata(MagnetInfo magnet)
    {
        using NetworkStream stream = new(_client, ownsSocket: false);
        BencodeToken requestDictionary = BencodeDecoder.Decode("d8:msg_typei0e5:piecei0ee"u8);
        byte[] requestMessagePayload = [Convert.ToByte(_peerMetadataExtensionId), .. requestDictionary.ToBencode()];
        byte[] message = new byte[requestMessagePayload.Length + 5];
        requestMessagePayload.CopyTo(message, 5);
        message[4] = 20;
        message[3] = Convert.ToByte(requestMessagePayload.Length+1);
        await _client.SendAsync(message, SocketFlags.None);
        byte[] responseLengthBytes = new byte[4];
        await stream.ReadExactlyAsync(responseLengthBytes);
        byte[] responseBytes = new byte[BinaryPrimitives.ReadInt32BigEndian(responseLengthBytes)];
        await stream.ReadExactlyAsync(responseBytes);
        BencodeDictionary response = BencodeDecoder.Decode(responseBytes[2..]) as BencodeDictionary;
        int metadataLength = Convert.ToInt32(((BencodeInteger)response["total_size"]).Value);
        byte[] metadataBytes = new byte[metadataLength];
        Buffer.BlockCopy(responseBytes, (int)((responseBytes.Length)-metadataLength), metadataBytes, 0, metadataLength);
        BencodeDictionary metadata = BencodeDecoder.Decode(metadataBytes) as BencodeDictionary;
        TorrentInfo torrent = TorrentInfo.FromMagnetBytes(metadataBytes, magnet.Announce);
        _torrent = torrent;
        return torrent;
    }

    public Task BeginMessageLoop()=> ReceiveLoopAsync();
    
    private async Task ReceiveLoopAsync()
    {
        if (_client is null) throw new InvalidOperationException("Not connected.");

        using NetworkStream stream = new(_client, ownsSocket: false);

        byte[] lengthBuffer = new byte[4];

        while (true)
        {
            // Wait until we have all four bytes of the length prefix.
            await stream.ReadExactlyAsync(lengthBuffer);

            int length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

            // Keep-alive message
            if (length == 0) continue;

            if (length < 0 || length > 1024 * 1024) throw new InvalidDataException("Invalid message length.");

            // Wait until the entire message has arrived.
            byte[] message = new byte[length];
            await stream.ReadExactlyAsync(message);

            // We now have one complete message.
            ProcessReceived(message);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private void ProcessReceived(byte[] message)
    {
        byte messageId = message[0];
        switch (messageId)
        {
            case 5:
                // bitfield - currently a no-op
                _bitfieldReceived.TrySetResult();
                break;
            case 1:
                // unchoked
                _unchoked.TrySetResult();
                break;
            case 7:
                // piece
                _blockReceived.TrySetResult(message[9..]);
                break;
            default:
                Console.WriteLine($"Unknown message id: {messageId}");
                break;
        } 
    }

    public async Task<byte[]> GetPiece(long pieceIndex)
    {
        if (!_loopStarted)
        {
            _ = BeginMessageLoop();
            _loopStarted = true;
        }
        //if (!_bitfieldReceived.Task.IsCompleted) await _bitfieldReceived.Task;    
        await SendInterestedMessageAsync();
        await _unchoked.Task;
        List<Block> blocks = GetBlocksForPiece(pieceIndex);
        byte[] pieceData = new byte[blocks.Sum(b => b.Length)];
        foreach (Block block in blocks)
        {
            TaskCompletionSource<byte[]> blockReceived = _blockReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await SendRequestMessage(pieceIndex, block.Offset, block.Length);
            byte[] blockData = await blockReceived.Task;
            blockData.CopyTo(pieceData, block.Offset);
        }
        return pieceData;
    }

    private async Task SendRequestMessage(long pieceIndex, int offset, int length)
    {
        byte[] message = new byte[17];
        BinaryPrimitives.WriteInt32BigEndian(message.AsSpan(0, 4), 13);
        message[4] = 6;
        BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(5, 4), (uint)pieceIndex);
        BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(9, 4), (uint)offset);
        BinaryPrimitives.WriteUInt32BigEndian(message.AsSpan(13, 4), (uint)length);
        await _client.SendAsync(message, SocketFlags.None);
    }
    
    private List<Block> GetBlocksForPiece(long pieceIndex)
    {
        List<Block> blocks = [];
        int length = (int)Math.Min(
            _torrent.PieceLength,
            _torrent.Length - pieceIndex * _torrent.PieceLength);
        int blockSize = 16384;
        int index = 0;
        while (index + blockSize < length)
        {
            // complete blocks
            blocks.Add(new Block(index, blockSize));
            index += blockSize;
        }
        // partial block
        int remaining =  length - index;
        blocks.Add(new Block(index, remaining));
        return blocks;
    }
    
    private async Task SendInterestedMessageAsync()
    {
        byte[] message = [0, 0, 0, 1, 2];
        await _client?.SendAsync(message)!;
    }


}