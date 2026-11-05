
using System.Text;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Tests;

[TestClass]
public class BencodeStringTests
{
    [TestMethod]
    [DataRow("0:", "", 2)]
    [DataRow("1:a", "a", 3)]
    [DataRow("4:spam", "spam", 6)]
    [DataRow("5:hello", "hello", 7)]
    [DataRow("3:123", "123", 5)]
    [DataRow("5:hellos", "hello", 7)]
    [DataRow("4:spam4:eggs", "spam", 6)]
    public void Decode_ValidString_ReturnsExpectedValue(
        string input, string expected, int expectedOffset)
    {
        int offset = 0;
        byte[] data = Encoding.ASCII.GetBytes(input);

        BencodeString result = BencodeString.Decode(data, ref offset);

        CollectionAssert.AreEqual(
            Encoding.ASCII.GetBytes(expected),
            result.Value);

        Assert.AreEqual(expectedOffset, offset);
    }

    [TestMethod]
    [DataRow(":")]
    [DataRow("x:hello")]
    [DataRow("-1:a")]
    [DataRow("+1:a")]
    [DataRow("00:")]
    [DataRow("01:a")]
    [DataRow("4spam")]
    [DataRow("4:spa")]
    [DataRow("5:")]
    [DataRow("2147483647:")]
    [DataRow("2147483648:")]
    [DataRow("999999999999999999999:")]
    [DataRow("3")]
    [DataRow("1")]
    public void Decode_InvalidString_ThrowsFormatException(string input)
    {
        byte[] data = Encoding.ASCII.GetBytes(input);
        int offset = 0;

        Assert.ThrowsExactly<FormatException>(() => BencodeString.Decode(data, ref offset));
        Assert.AreEqual(0, offset);
    }

    [TestMethod]
    [DataRow("", 0)]
    [DataRow("0:", -1)]
    [DataRow("0:", 2)]
    [DataRow("0:", 3)]
    [DataRow("x4:ab", 1)]
    public void Decode_InvalidOffsetOrString_DoesNotAdvanceOffset(string input, int initialOffset)
    {
        byte[] data = Encoding.ASCII.GetBytes(input);
        int offset = initialOffset;

        Assert.ThrowsExactly<FormatException>(() => BencodeString.Decode(data, ref offset));
        Assert.AreEqual(initialOffset, offset);
    }

    [TestMethod]
    public void Decode_BinaryData_PreservesBytes()
    {
        byte[] data =
        [
            (byte)'4', (byte)':',
            0x00, 0xFF, 0x80, 0x01
        ];

        int offset = 0;

        BencodeString result = BencodeString.Decode(data, ref offset);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0xFF, 0x80, 0x01 },
            result.Value);

        Assert.AreEqual(6, offset);
    }

    [TestMethod]
    public void Decode_Utf8Data_UsesByteLength()
    {
        byte[] data = "2:é"u8.ToArray();

        int offset = 0;

        BencodeString result = BencodeString.Decode(data, ref offset);

        CollectionAssert.AreEqual("é"u8.ToArray(), result.Value);

        Assert.AreEqual(4, offset);
    }

    [TestMethod]
    [DataRow("4:spam4:eggs", 0, "spam", 6)]
    [DataRow("4:spam4:eggs", 6, "eggs", 12)]
    [DataRow("sc3:cats", 2, "cat", 7)]
    [DataRow("0:extra", 0, "", 2)]
    public void Decode_AtOffset_ConsumesExactlyOneString(
        string input,
        int initialOffset,
        string expected,
        int expectedOffset)
    {
        byte[] data = Encoding.ASCII.GetBytes(input);
        int offset = initialOffset;

        BencodeString result = BencodeString.Decode(data, ref offset);

        CollectionAssert.AreEqual(
            Encoding.ASCII.GetBytes(expected),
            result.Value);

        Assert.AreEqual(expectedOffset, offset);
    }

    [TestMethod]
    public void Decode_ThenEncode_ProducesIdenticalBytes()
    {
        byte[] original =
        [
            (byte)'4', (byte)':',
            0x00, 0xFF, 0x80, 0x01
        ];

        int offset = 0;

        BencodeString decoded = BencodeString.Decode(original, ref offset);

        CollectionAssert.AreEqual(original, decoded.ToBencode());
        Assert.AreEqual(original.Length, offset);
    }
}
