using System.Text;
using CodeCrafters.Bittorrent.Bencode;

namespace CodeCrafters.Bittorrent.Tests;

[TestClass]
public class BencodeDictionaryTests
{
    [TestMethod]
    public void Decode_EmptyDictionary_ReturnsEmptyDictionary()
    {
        byte[] data = "de"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);

        Assert.HasCount(0, result.Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_SingleString_ReturnsString()
    {
        byte[] data = "d3:foo3:bare"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString key = new("foo"u8.ToArray());
        BencodeString value = (BencodeString)result.Value[key];

        Assert.HasCount(1, result.Value);
        Assert.IsTrue(result.Value.ContainsKey(key));
        CollectionAssert.AreEqual("bar"u8.ToArray(), value.Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_SingleInteger_ReturnsInteger()
    {
        byte[] data = "d3:agei42ee"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString key = new("age"u8.ToArray());

        Assert.HasCount(1, result.Value);
        Assert.AreEqual(42L, ((BencodeInteger)result.Value[key]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_MixedTypes_ReturnsCorrectValues()
    {
        byte[] data = "d3:agei42e4:name4:Joshe"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString ageKey = new("age"u8.ToArray());
        BencodeString nameKey = new("name"u8.ToArray());

        Assert.HasCount(2, result.Value);
        Assert.AreEqual(42L, ((BencodeInteger)result.Value[ageKey]).Value);
        CollectionAssert.AreEqual("Josh"u8.ToArray(), ((BencodeString)result.Value[nameKey]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_NestedDictionary_ReturnsNestedValues()
    {
        byte[] data = "d4:infod3:foo3:baree"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString infoKey = new("info"u8.ToArray());
        BencodeString fooKey = new("foo"u8.ToArray());
        BencodeDictionary nested = (BencodeDictionary)result.Value[infoKey];

        Assert.HasCount(1, result.Value);
        Assert.HasCount(1, nested.Value);
        CollectionAssert.AreEqual("bar"u8.ToArray(), ((BencodeString)nested.Value[fooKey]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_DictionaryContainingList_ReturnsList()
    {
        byte[] data = "d4:listli1ei2ei3eee"u8.ToArray();
        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString key = new("list"u8.ToArray());
        BencodeList list = (BencodeList)result.Value[key];

        Assert.HasCount(3, list.Value);
        Assert.AreEqual(1L, ((BencodeInteger)list.Value[0]).Value);
        Assert.AreEqual(2L, ((BencodeInteger)list.Value[1]).Value);
        Assert.AreEqual(3L, ((BencodeInteger)list.Value[2]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_ListContainingDictionaries_ReturnsDictionaries()
    {
        byte[] data = "ld1:ai1eed1:bi2eee"u8.ToArray();
        int offset = 0;

        BencodeList result = BencodeList.Decode(data, ref offset);
        BencodeDictionary first = (BencodeDictionary)result.Value[0];
        BencodeDictionary second = (BencodeDictionary)result.Value[1];
        BencodeString firstKey = new("a"u8.ToArray());
        BencodeString secondKey = new("b"u8.ToArray());

        Assert.HasCount(2, result.Value);
        Assert.AreEqual(1L, ((BencodeInteger)first.Value[firstKey]).Value);
        Assert.AreEqual(2L, ((BencodeInteger)second.Value[secondKey]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_AtOffset_ConsumesExactlyOneDictionary()
    {
        byte[] data = "xxd3:foo3:bareyy"u8.ToArray();
        int offset = 2;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);

        Assert.HasCount(1, result.Value);
        Assert.AreEqual(14, offset);
    }

    [TestMethod]
    public void Decode_ConsecutiveDictionaries_ConsumesOnlyFirst()
    {
        byte[] data = "d1:ai1eed1:bi2ee"u8.ToArray();
        int offset = 0;

        BencodeString firstKey = new("a"u8.ToArray());
        BencodeDictionary first = BencodeDictionary.Decode(data, ref offset);
        Assert.AreEqual(1L, ((BencodeInteger)first.Value[firstKey]).Value);
        Assert.AreEqual(8, offset);

        BencodeString secondKey = new("b"u8.ToArray());
        BencodeDictionary second = BencodeDictionary.Decode(data, ref offset);
        Assert.AreEqual(2L, ((BencodeInteger)second.Value[secondKey]).Value);
        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_BinaryString_PreservesBytes()
    {
        byte[] data =
        [
            (byte)'d',
            (byte)'4', (byte)':',
            (byte)'d', (byte)'a', (byte)'t', (byte)'a',
            (byte)'3', (byte)':',
            0x00, 0xFF, 0x80,
            (byte)'e'
        ];

        int offset = 0;

        BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
        BencodeString key = new("data"u8.ToArray());

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0xFF, 0x80 },
            ((BencodeString)result.Value[key]).Value);

        Assert.AreEqual(data.Length, offset);
    }

    [TestMethod]
    public void Decode_ThenEncode_ProducesIdenticalBytes()
    {
        byte[] original = "d3:agei42e4:infod3:foo3:bare4:listli1ei2eee"u8.ToArray();

        int offset = 0;

        BencodeDictionary decoded =
            BencodeDictionary.Decode(original, ref offset);

        CollectionAssert.AreEqual(original, decoded.ToBencode());
        Assert.AreEqual(original.Length, offset);
    }

    [TestMethod]
    public void Encode_UnsortedDictionary_EncodesKeysInCanonicalOrder()
    {
        BencodeDictionary dictionary = new(new Dictionary<BencodeString, BencodeToken>
        {
            [new BencodeString("z"u8.ToArray())] = new BencodeInteger(1),
            [new BencodeString("a"u8.ToArray())] = new BencodeInteger(2)
        });

        byte[] encoded = dictionary.ToBencode();

        CollectionAssert.AreEqual("d1:ai2e1:zi1ee"u8.ToArray(), encoded);
    }

    [TestMethod]
    public void Encode_UnicodeKeys_UsesUtf8ByteLengthsAndOrdering()
    {
        BencodeDictionary dictionary = new(new Dictionary<BencodeString, BencodeToken>
        {
            [new BencodeString("\U00010000"u8.ToArray())] = new BencodeInteger(2),
            [new BencodeString("\uE000"u8.ToArray())] = new BencodeInteger(1)
        });

        byte[] encoded = dictionary.ToBencode();

        CollectionAssert.AreEqual("d3:\uE000i1e4:\U00010000i2ee"u8.ToArray(), encoded);
    }

    [TestClass]
    public class BencodeDictionaryInvalidTests
    {
        [TestMethod]
        [DataRow("d")]                    // Missing closing e
        [DataRow("d3:foo3:bar")]          // Unterminated dictionary
        [DataRow("d3:fore")]              // Key without value
        [DataRow("di1e3:fore")]          // Integer used as key
        [DataRow("d4:fuji3:bar4:fuji1ee")] // Duplicate key
        [DataRow("d1:bi1e1:ai2ee")]      // Keys out of order
        [DataRow("d3:foo4:bare")]        // Truncated string value
        [DataRow("d3:fuji42e")]          // Missing dictionary terminator
        [DataRow("d4:\U00010000i1e3:\uE000i2ee")] // Unicode keys out of byte order
        public void Decode_InvalidDictionary_ThrowsFormatException(
            string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            int offset = 0;

            Assert.ThrowsExactly<FormatException>(
                () => BencodeDictionary.Decode(data, ref offset));
            Assert.AreEqual(0, offset);
        }

        [TestMethod]
        [DataRow("", 0)]
        [DataRow("de", -1)]
        [DataRow("de", 2)]
        [DataRow("xe", 0)]
        public void Decode_InvalidPrefixOrOffset_ThrowsFormatExceptionWithoutAdvancing(
            string input, int initialOffset)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);
            int offset = initialOffset;

            Assert.ThrowsExactly<FormatException>(
                () => BencodeDictionary.Decode(data, ref offset));
            Assert.AreEqual(initialOffset, offset);
        }

        [TestMethod]
        public void Decode_InvalidUtf8Key_PreservesBytes()
        {
            byte[] data =
            [
                (byte)'d', (byte)'1', (byte)':', 0xFF,
                (byte)'i', (byte)'1', (byte)'e', (byte)'e'
            ];
            int offset = 0;

            BencodeDictionary result = BencodeDictionary.Decode(data, ref offset);
            BencodeString key = new([0xFF]);

            Assert.HasCount(1, result.Value);
            Assert.IsTrue(result.Value.ContainsKey(key));
            Assert.AreEqual(1L, ((BencodeInteger)result.Value[key]).Value);
            Assert.AreEqual(data.Length, offset);
        }
    }

}