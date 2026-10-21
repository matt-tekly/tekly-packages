// Fixture classes here are populated by the serializer, not by code, so the compiler can't see the assignments.
#pragma warning disable CS0649

using System.Collections.Generic;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Tests
{
	public class NullTests
	{
		[Test]
		public void NullTopLevelObject()
		{
			Assert.IsNull(Serial.RoundTrip<Pair>(null));
		}

		[Test]
		public void NullTopLevelString()
		{
			Assert.IsNull(Serial.RoundTrip<string>(null));
		}

		[Test]
		public void NullTopLevelIsOneByte()
		{
			using var memory = Serial.Write(null);
			CollectionAssert.AreEqual(new[] { (byte) WireType.Null }, Serial.Body(memory));
		}

		[Test]
		public void NullFieldsOfEveryKind()
		{
			var actual = Serial.RoundTrip(new Nullables());

			Assert.IsNull(actual.Object);
			Assert.IsNull(actual.String);
			Assert.IsNull(actual.List);
			Assert.IsNull(actual.Array);
			Assert.IsNull(actual.Bytes);
			Assert.IsNull(actual.Dictionary);
			Assert.IsNull(actual.NullableInt);
			Assert.IsNull(actual.NullableEnum);
		}

		[Test]
		public void NullFieldsClearAnExistingObject()
		{
			var existing = new Nullables {
				Object = new Pair(1, "a"),
				String = "stale",
				List = new List<int> { 1 },
				NullableInt = 5,
			};

			using var memory = Serial.Write(new Nullables());
			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			Assert.IsNull(actual.Object);
			Assert.IsNull(actual.String);
			Assert.IsNull(actual.List);
			Assert.IsNull(actual.NullableInt);
		}

		[Test]
		public void NullReadIntoAValueTypeFieldBecomesDefault()
		{
			// Written from a class whose field is int?, read into one whose same named field is int.
			using var memory = Serial.Write(new NullableHolder { Value = null });
			var actual = Serial.Read<IntHolder>(memory, existing: new IntHolder { Value = 42 });

			Assert.AreEqual(0, actual.Value);
		}

		[Test]
		public void EmptyStringIsNotNull()
		{
			var actual = Serial.RoundTrip(new Nullables { String = "" });
			Assert.AreEqual("", actual.String);
		}

		[Test]
		public void NullDictionaryKeyIsRejected()
		{
			using var memory = new System.IO.MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				output.WriteWireType(WireType.Map);
				output.WriteVarUInt(1);
				output.WriteWireType(WireType.Null);
				output.WriteWireType(WireType.Varint);
				output.WriteVarInt(1);
			}

			Assert.Throws<SuperSerialException>(() => Serial.Read<Dictionary<string, int>>(memory));
		}

		private class Nullables
		{
			public Pair Object;
			public string String;
			public List<int> List;
			public int[] Array;
			public byte[] Bytes;
			public Dictionary<string, int> Dictionary;
			public int? NullableInt;
			public Rarity? NullableEnum;
		}

		private class NullableHolder
		{
			public int? Value;
		}

		private class IntHolder
		{
			public int Value;
		}
	}
}
