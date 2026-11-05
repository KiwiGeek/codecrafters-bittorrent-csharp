using System.Text;
using CodeCrafters.Bittorrent.Bencode;
namespace CodeCrafters.Bittorrent.Tests;

[TestClass]
public class IntegerTests
{

    [TestMethod]
    [DataRow("i0e", 0L, 3)]
    [DataRow("i42e", 42L, 4)]
    [DataRow("i-42e", -42L, 5)]
    [DataRow("i9223372036854775807e", long.MaxValue, 21)]
    [DataRow("i-9223372036854775808e", long.MinValue, 22)]
    public void Decode_ValidInteger_ReturnsExpectedValue(
        string input, long expected, int expectedOffset)
    {
        int offset = 0;
        byte[] data = Encoding.ASCII.GetBytes(input);

        BencodeInteger result = BencodeInteger.Decode(data, ref offset);

        Assert.AreEqual(expected, result.Value);
        Assert.AreEqual(expectedOffset, offset);
    }

    [TestMethod]
    public void Decode_ValidIntegerFromNonzeroOffset_AdvancesOffset()
    {
        byte[] data = "xi42e"u8.ToArray();
        int offset = 1;

        BencodeInteger result = BencodeInteger.Decode(data, ref offset);

        Assert.AreEqual(42L, result.Value);
        Assert.AreEqual(5, offset);
    }

    [TestMethod]
    [DataRow("ie")]
    [DataRow("i-e")]
    [DataRow("i-0e")]
    [DataRow("i00e")]
    [DataRow("i01e")]
    [DataRow("i-01e")]
    [DataRow("i+1e")]
    [DataRow("i1.5e")]
    [DataRow("i12")]
    [DataRow("i12xe")]
    [DataRow("i 12e")]
    [DataRow("i9223372036854775808e")]
    [DataRow("i-9223372036854775809e")]
    public void Decode_InvalidInteger_ThrowsFormatException(string input)
    {
        int offset = 0;
        byte[] data = Encoding.ASCII.GetBytes(input);

        Assert.ThrowsExactly<FormatException>(
            () => BencodeInteger.Decode(data, ref offset));
        Assert.AreEqual(0, offset);
    }

    [TestMethod]
    [DataRow("xi1e", 0)]
    [DataRow("i1e", -1)]
    [DataRow("i1e", 3)]
    public void Decode_InvalidPrefixOrOffset_ThrowsFormatExceptionWithoutAdvancing(
        string input, int initialOffset)
    {
        int offset = initialOffset;
        byte[] data = Encoding.ASCII.GetBytes(input);

        Assert.ThrowsExactly<FormatException>(
            () => BencodeInteger.Decode(data, ref offset));
        Assert.AreEqual(initialOffset, offset);
    }
}