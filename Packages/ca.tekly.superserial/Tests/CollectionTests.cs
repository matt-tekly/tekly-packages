using System.Collections.Generic;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;

namespace Tekly.SuperSerial.Tests
{
	public class CollectionTests
	{
		[Test]
		public void ListOfInts()
		{
			var expected = new List<int> { 3, 2, 1 };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void EmptyList()
		{
			var actual = Serial.RoundTrip(new List<int>());
			Assert.IsNotNull(actual);
			Assert.IsEmpty(actual);
		}

		[Test]
		public void ArrayOfInts()
		{
			var expected = new[] { 3, 2, 1 };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ArrayOfBools()
		{
			var expected = new[] { true, false, true, true, false };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ArrayOfEnums()
		{
			var expected = new[] { Rarity.Epic, Rarity.Common };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ByteArrayIsWrittenAsBytes()
		{
			var expected = new byte[] { 1, 2, 3, 250 };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));

			using var memory = Serial.Write(expected);
			// Bytes tag, length, four bytes. Not a List of four tagged values.
			Assert.AreEqual(1 + 1 + 4, Serial.Body(memory).Length);
		}

		[Test]
		public void ListOfStringsWithNulls()
		{
			var expected = new List<string> { null, "x", "" };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ListOfObjectsWithNulls()
		{
			var expected = new List<Pair> { new Pair(1, "a"), null, new Pair(2, "b") };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ListOfValueTypes()
		{
			var expected = new List<ValueTypes> { ValueTypes.Create(1), ValueTypes.Create(2) };
			CollectionAssert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void ListOfStructs()
		{
			var expected = new List<Point> { new Point(1, 2), new Point(3, 4) };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(2, actual.Count);
			Assert.AreEqual(1, actual[0].X);
			Assert.AreEqual(4, actual[1].Y);
		}

		[Test]
		public void JaggedArray()
		{
			var expected = new[] { new[] { 1, 2 }, null, new int[0] };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(3, actual.Length);
			CollectionAssert.AreEqual(expected[0], actual[0]);
			Assert.IsNull(actual[1]);
			Assert.IsEmpty(actual[2]);
		}

		[Test]
		public void MultidimensionalArraysAreRejected()
		{
			Assert.Throws<SuperSerialException>(() => Serial.Write(new int[2, 2]));
		}

		[Test]
		public void DictionaryStringToInt()
		{
			var expected = new Dictionary<string, int> { { "sword", 1 }, { "potion", 12 } };
			CollectionAssert.AreEquivalent(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void DictionaryEnumToListWithNullValue()
		{
			var expected = new Dictionary<Rarity, List<string>> {
				{ Rarity.Rare, new List<string> { "gem" } },
				{ Rarity.Common, null },
			};

			var actual = Serial.RoundTrip(expected);

			CollectionAssert.AreEqual(expected[Rarity.Rare], actual[Rarity.Rare]);
			Assert.IsNull(actual[Rarity.Common]);
		}

		[Test]
		public void DictionaryWithStructKey()
		{
			var expected = new Dictionary<Point, string> { { new Point(1, 2), "a" }, { new Point(3, 4), "b" } };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(2, actual.Count);
			Assert.AreEqual("a", actual[new Point(1, 2)]);
			Assert.AreEqual("b", actual[new Point(3, 4)]);
		}

		[Test]
		public void ListOfObjectHoldsMixedTypes()
		{
			var expected = new List<object> { 1, "two", 3.0f, new Pair(4, "four"), null };

			var actual = Serial.RoundTripFresh(expected, reader => reader.RegisterType<Pair>());

			CollectionAssert.AreEqual(expected, actual);
		}

		[Test]
		public void CollectionsAsFields()
		{
			var expected = new Inventory {
				Counts = new List<int> { 1, 2 },
				Slots = new[] { 3, 4, 5 },
				Blob = new byte[] { 6 },
				Stock = new Dictionary<string, int> { { "a", 7 } },
			};

			var actual = Serial.RoundTrip(expected);

			CollectionAssert.AreEqual(expected.Counts, actual.Counts);
			CollectionAssert.AreEqual(expected.Slots, actual.Slots);
			CollectionAssert.AreEqual(expected.Blob, actual.Blob);
			CollectionAssert.AreEquivalent(expected.Stock, actual.Stock);
		}

		[Test]
		public void ExistingListIsReusedAndCleared()
		{
			var existing = new List<int> { 9, 9, 9 };
			using var memory = Serial.Write(new List<int> { 1, 2 });

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			CollectionAssert.AreEqual(new[] { 1, 2 }, actual);
		}

		[Test]
		public void ExistingArrayIsReusedWhenTheLengthMatches()
		{
			var existing = new int[2];
			using var memory = Serial.Write(new[] { 1, 2 });

			Assert.AreSame(existing, Serial.Read(memory, existing: existing));
		}

		[Test]
		public void ExistingArrayIsReplacedWhenTheLengthDiffers()
		{
			var existing = new int[3];
			using var memory = Serial.Write(new[] { 1, 2 });

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreNotSame(existing, actual);
			Assert.AreEqual(2, actual.Length);
		}

		[Test]
		public void ExistingDictionaryIsReusedAndCleared()
		{
			var existing = new Dictionary<string, int> { { "stale", 1 } };
			using var memory = Serial.Write(new Dictionary<string, int> { { "fresh", 2 } });

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			CollectionAssert.AreEquivalent(new Dictionary<string, int> { { "fresh", 2 } }, actual);
		}

		private class Inventory
		{
			public List<int> Counts;
			public int[] Slots;
			public byte[] Blob;
			public Dictionary<string, int> Stock;
		}
	}
}
