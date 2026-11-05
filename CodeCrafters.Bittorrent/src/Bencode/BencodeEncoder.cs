namespace CodeCrafters.Bittorrent.Bencode;

public static class BencodeEncoder
{
    public static byte[] Encode(BencodeToken token) => token.ToBencode();
}