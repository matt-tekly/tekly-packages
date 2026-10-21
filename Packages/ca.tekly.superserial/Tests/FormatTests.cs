using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// Pins the bytes the serializer produces, so a change to the format is a deliberate test change and a
	/// symmetric bug in writer and reader can't hide behind a round trip.
	/// </summary>
	public class FormatTests
	{
		[Test]
		public void Header()
		{
			using var memory = Serial.Write(1);
			var bytes = memory.ToArray();

			CollectionAssert.AreEqual(new[] { (byte) 'T', (byte) 'K', (byte) 'S', (byte) 'S', TokenOutputStream.VERSION, (byte) 0 }, bytes[..6]);
		}

		[Test]
		public void ObjectWithTwoFields()
		{
			using var memory = Serial.Write(new Pair(5, "hi"));

			var expected = new List<byte> { (byte) WireType.Object };
			expected.AddRange(Hash(nameof(Pair.Number)));
			expected.Add((byte) WireType.Varint);
			expected.Add(10); // zigzag(5)
			expected.AddRange(Hash(nameof(Pair.Text)));
			expected.Add((byte) WireType.Bytes);
			expected.Add(2);
			expected.Add((byte) 'h');
			expected.Add((byte) 'i');
			expected.AddRange(new byte[] { 0, 0, 0, 0 }); // end of object

			CollectionAssert.AreEqual(expected, Serial.Body(memory));
		}

		[Test]
		public void NegativeIntIsZigzagged()
		{
			using var memory = Serial.Write(-1);
			CollectionAssert.AreEqual(new[] { (byte) WireType.Varint, (byte) 1 }, Serial.Body(memory));
		}

		[Test]
		public void LargeIntTakesMoreBytes()
		{
			using var memory = Serial.Write(300);
			// zigzag(300) = 600 = 0b1001011000 -> 0xD8 0x04
			CollectionAssert.AreEqual(new[] { (byte) WireType.Varint, (byte) 0xD8, (byte) 0x04 }, Serial.Body(memory));
		}

		[Test]
		public void FloatIsFixed32LittleEndian()
		{
			using var memory = Serial.Write(1f);
			CollectionAssert.AreEqual(new[] { (byte) WireType.Fixed32, (byte) 0, (byte) 0, (byte) 0x80, (byte) 0x3F }, Serial.Body(memory));
		}

		[Test]
		public void EnumIsOneByte()
		{
			using var memory = Serial.Write(Rarity.Epic);
			CollectionAssert.AreEqual(new[] { (byte) WireType.Varint, (byte) 4 }, Serial.Body(memory));
		}

		[Test]
		public void ListOfEnumsIsOneTagPlusOneBytePerElement()
		{
			using var memory = Serial.Write(new[] { Rarity.Common, Rarity.Epic });
			CollectionAssert.AreEqual(new[] { (byte) WireType.List, (byte) 2, (byte) WireType.Varint, (byte) 0, (byte) WireType.Varint, (byte) 4 }, Serial.Body(memory));
		}

		[Test]
		public void StringIsBytesWithVarintLength()
		{
			using var memory = Serial.Write("ab");
			CollectionAssert.AreEqual(new[] { (byte) WireType.Bytes, (byte) 2, (byte) 'a', (byte) 'b' }, Serial.Body(memory));
		}

		[Test]
		public void PolymorphicValueCarriesATypeHash()
		{
			var serializer = new SuperSerializer();
			serializer.RegisterType<Square>();

			using var memory = Serial.Write(new Holder { Shape = new Square() }, serializer);
			var body = Serial.Body(memory);

			var expected = new List<byte> { (byte) WireType.Object };
			expected.AddRange(Hash(nameof(Holder.Shape)));
			expected.Add((byte) WireType.TypedObject);
			expected.AddRange(Hash("Tests.Square")); // the [SerialName] on Square
			expected.Add((byte) WireType.Object);

			CollectionAssert.AreEqual(expected, body[..expected.Count]);
		}

		[Test]
		public void TopLevelISuperSerializeIsRaw()
		{
			using var memory = Serial.Write(new SerialStruct { Value = 1, Label = "a" });
			var body = Serial.Body(memory);

			Assert.AreEqual((byte) WireType.Raw, body[0]);
			Assert.AreEqual(1 + 4 + 1 + 1, body.Length, "tag, int, string length, one char, no length prefix");
		}

		[Test]
		public void NestedISuperSerializeIsALengthPrefixedBlock()
		{
			using var memory = Serial.Write(new Holder { Serial = new SerialStruct { Value = 1, Label = "a" } });
			var body = Serial.Body(memory);

			var expected = new List<byte> { (byte) WireType.Object };
			expected.AddRange(Hash(nameof(Holder.Shape)));
			expected.Add((byte) WireType.Null);
			expected.AddRange(Hash(nameof(Holder.Serial)));
			expected.Add((byte) WireType.Block);
			expected.AddRange(new byte[] { 6, 0, 0, 0 }); // int + varint(2) + 'a'
			expected.AddRange(new byte[] { 1, 0, 0, 0 });
			expected.Add(2);
			expected.Add((byte) 'a');

			CollectionAssert.AreEqual(expected, body[..expected.Count]);
		}

		private static byte[] Hash(string name)
		{
			var bytes = new byte[4];
			BinaryPrimitives.WriteUInt32LittleEndian(bytes, XXHash.Hash32(name));
			return bytes;
		}

		private class Holder
		{
			public Shape Shape;
			public SerialStruct Serial;
		}
	}
}
