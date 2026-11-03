using System.Text;

namespace CodeCrafters.Bittorrent.Bencode;

public static class BencodeEncoder
{
    public static byte[] Encode(BencodeToken token)
    {
        List<byte> output = [];
        switch (token)
        {
            case BencodeInteger integer:
                output.Add(Convert.ToByte('i'));
                foreach (char c in integer.Value.ToString())
                {
                    output.Add(Convert.ToByte(c));
                }
                output.Add(Convert.ToByte('e'));
                break;
            case BencodeString str:
                string strLength = str.Value.Length.ToString();
                foreach (char c in strLength)
                {
                    output.Add(Convert.ToByte(c));
                }
                output.Add(Convert.ToByte(':'));
                foreach (char c in str.Value)
                {
                    output.Add(Convert.ToByte(c));
                }
                break;
            case BencodeList list:
                output.Add(Convert.ToByte('l'));
                foreach (BencodeToken bt in list.Value)
                {
                    byte[] newBytes = Encode(bt);
                    output.AddRange(newBytes);
                }
                output.Add(Convert.ToByte('e'));
                break;
            case BencodeDictionary dict:
                output.Add(Convert.ToByte('d'));
                foreach ((string key, BencodeToken value) in dict.Values)
                {
                    string dictKeyLength = key.Length.ToString();
                    foreach (char c in dictKeyLength)
                    {
                        output.Add(Convert.ToByte(c));
                    }
                    output.Add(Convert.ToByte(':'));
                    foreach (char c in key)
                    {
                        output.Add(Convert.ToByte(c));
                    }
                    byte[] newBytes = Encode(value);
                    output.AddRange(newBytes);
                }
                output.Add(Convert.ToByte('e'));
                break;
            default:
                throw new NotImplementedException();
        }
        return output.ToArray();
    }
}