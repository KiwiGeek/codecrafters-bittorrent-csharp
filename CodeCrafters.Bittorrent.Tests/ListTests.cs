
using System.Text;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Tests;

[TestClass]
public class BencodeListTests
{
    [TestMethod]
    public void Decode_EmptyList_ReturnsEmptyList()
    {
        byte[] data = Encoding.ASCII.GetBytes("le");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(0, result.Value);
        Assert.AreEqual(2, offset);
    }

    [TestMethod]
    public void Decode_SingleInteger_ReturnsInteger()
    {
        byte[] data = Encoding.ASCII.GetBytes("li42ee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);
        Assert.IsInstanceOfType<BencodeInteger>(result.Value[0]);
        Assert.AreEqual(42L, ((BencodeInteger)result.Value[0]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_SingleString_ReturnsString()
    {
        byte[] data = Encoding.ASCII.GetBytes("l4:lovee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);
        Assert.IsInstanceOfType<BencodeString>(result.Value[0]);

        CollectionAssert.AreEqual(
            Encoding.ASCII.GetBytes("love"),
            ((BencodeString)result.Value[0]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_MixedTypes_ReturnsCorrectValues()
    {
        byte[] data = Encoding.ASCII.GetBytes("li42e3:taxi-7ee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(3, result.Value);

        Assert.AreEqual(42L, ((BencodeInteger)result.Value[0]).Value);

        CollectionAssert.AreEqual(
            Encoding.ASCII.GetBytes("tax"),
            ((BencodeString)result.Value[1]).Value);

        Assert.AreEqual(-7L, ((BencodeInteger)result.Value[2]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_NestedList_ReturnsNestedValues()
    {
        byte[] data = Encoding.ASCII.GetBytes("lli1ei2eee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);

        BencodeList nested = (BencodeList)result.Value[0];

        Assert.HasCount(2, nested.Value);
        Assert.AreEqual(1L, ((BencodeInteger)nested.Value[0]).Value);
        Assert.AreEqual(2L, ((BencodeInteger)nested.Value[1]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_MultipleNestedLists_ReturnsCorrectStructure()
    {
        byte[] data = Encoding.ASCII.GetBytes("lli1eeli2eee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(2, result.Value);

        BencodeList first = (BencodeList)result.Value[0];
        BencodeList second = (BencodeList)result.Value[1];

        Assert.AreEqual(1L, ((BencodeInteger)first.Value[0]).Value);
        Assert.AreEqual(2L, ((BencodeInteger)second.Value[0]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_NestedEmptyLists_ReturnsEmptyLists()
    {
        // ReSharper disable once StringLiteralTypo  - I wish I could find a valid word for this one :D
        byte[] data = Encoding.ASCII.GetBytes("llelee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(2, result.Value);

        Assert.HasCount(0, ((BencodeList)result.Value[0]).Value);
        Assert.HasCount(0, ((BencodeList)result.Value[1]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_AtOffset_ConsumesExactlyOneList()
    {
        byte[] data = Encoding.ASCII.GetBytes("alli42eeball");
        int offset = 2;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);
        Assert.AreEqual(42L, ((BencodeInteger)result.Value[0]).Value);
        Assert.AreEqual(8, offset);
    }

    [TestMethod]
    public void Decode_ConsecutiveLists_ConsumesOnlyFirstList()
    {
        // ReSharper disable once StringLiteralTypo  - I wish I could find a valid word for this one :D
        byte[] data = Encoding.ASCII.GetBytes("li1eeli2ee");
        int offset = 0;

        BencodeList first = BencodeList.Decode(data, ref offset);

        Assert.AreEqual(1L, ((BencodeInteger)first.Value[0]).Value);
        Assert.AreEqual(5, offset);

        BencodeList second = BencodeList.Decode(data, ref offset);

        Assert.AreEqual(2L, ((BencodeInteger)second.Value[0]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_StringContainingControlCharacters_DoesNotParseThem()
    {
        byte[] data = Encoding.ASCII.GetBytes("l2:bee");
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);

        CollectionAssert.AreEqual(
            Encoding.ASCII.GetBytes("be"),
            ((BencodeString)result.Value[0]).Value);

        Assert.AreEqual(data.Length, offset);
    }
    
    [TestMethod]
    public void Decode_UnterminatedList_ThrowsFormatException()
    {
        byte[] data = Encoding.ASCII.GetBytes("l4:idle");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            int offset = 0;
            BencodeList.Decode(data, ref offset);
        });
    }

    [TestMethod]
    public void Decode_ThenEncode_ProducesIdenticalBytes()
    {
        byte[] original = Encoding.ASCII.GetBytes(
            "li42e6:broccoli1ei2eee");

        int offset = 0;

        BencodeList decoded = BencodeList.Decode(original, ref offset);

        CollectionAssert.AreEqual(original, decoded.ToBencode());
        Assert.AreEqual(original.Length, offset);
    }

    [TestMethod]
    public void Decode_BinaryString_PreservesBytes()
    {
        byte[] data =
        [
            (byte)'l',
            (byte)'3', (byte)':',
            0x00, 0xFF, 0x80,
            (byte)'e'
        ];

        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0xFF, 0x80 },
            ((BencodeString)result.Value[0]).Value);

        Assert.AreEqual(data.Length, offset);
    }
}
