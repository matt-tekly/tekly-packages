using System;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	public class ValueTests
	{
		[Test]
		public void EveryValueTypeRoundTrips()
		{
			var expected = ValueTypes.Create();
			Assert.AreEqual(expected, Serial.RoundTrip(expected));
		}

		[Test]
		public void EveryValueTypeRoundTripsThroughATricklingStream()
		{
			var expected = ValueTypes.Create();
			using var memory = Serial.Write(expected);
			using var input = new TokenInputStream(new TrickleStream(memory), true);
			Assert.AreEqual(expected, new SuperSerializer().Read<ValueTypes>(input));
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(-1)]
		[TestCase(int.MaxValue)]
		[TestCase(int.MinValue)]
		public void Int(int value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase(0L)]
		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		public void Long(long value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase(ulong.MaxValue)]
		[TestCase(0UL)]
		public void ULong(ulong value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Bool(bool value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase(0f)]
		[TestCase(-1.5f)]
		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		public void Float(float value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase(Math.PI)]
		[TestCase(double.Epsilon)]
		public void Double(double value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[TestCase("")]
		[TestCase("ascii")]
		[TestCase("héllo wörld – 日本語 🎮")]
		public void String(string value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[Test]
		public void LongString()
		{
			var value = new string('x', 20_000);
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[Test]
		public void Decimal()
		{
			Assert.AreEqual(decimal.MaxValue, Serial.RoundTrip(decimal.MaxValue));
			Assert.AreEqual(-0.0001m, Serial.RoundTrip(-0.0001m));
		}

		[Test]
		public void Guid()
		{
			var value = System.Guid.NewGuid();
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[Test]
		public void DateTimeKeepsItsKind()
		{
			var utc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
			var local = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);

			Assert.AreEqual(DateTimeKind.Utc, Serial.RoundTrip(utc).Kind);
			Assert.AreEqual(DateTimeKind.Local, Serial.RoundTrip(local).Kind);
		}

		[TestCase(Rarity.Common)]
		[TestCase(Rarity.Epic)]
		public void Enum(Rarity value)
		{
			Assert.AreEqual(value, Serial.RoundTrip(value));
		}

		[Test]
		public void EnumWithULongUnderlyingType()
		{
			Assert.AreEqual(Flags64.Huge, Serial.RoundTrip(Flags64.Huge));
		}

		[Test]
		public void NullableWithValue()
		{
			int? value = 5;
			Assert.AreEqual(5, Serial.RoundTrip(value));
		}

		[Test]
		public void NullableWithoutValue()
		{
			int? value = null;
			Assert.IsNull(Serial.RoundTrip(value));
		}

		[Test]
		public void StructThroughReflection()
		{
			var expected = new PlainStruct { A = 7, B = "s", C = new Vector2(1, 2) };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(expected.A, actual.A);
			Assert.AreEqual(expected.B, actual.B);
			Assert.AreEqual(expected.C, actual.C);
		}

		[Test]
		public void StructAsAField()
		{
			var expected = new Holder { Plain = new PlainStruct { A = 1, B = "b", C = new Vector2(2, 3) } };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(expected.Plain, actual.Plain);
		}

		[Test]
		public void UnityTypesWithTheWrongPayloadLengthFallBackToDefault()
		{
			// A Vector3 written as a Bytes payload of 8 bytes instead of 12. The reader skips it and uses default.
			using var memory = new System.IO.MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				output.WriteWireType(WireType.Bytes);
				output.WriteVarUInt(8);
				output.Write(1f);
				output.Write(2f);
			}

			Assert.AreEqual(Vector3.zero, Serial.Read<Vector3>(memory));
		}

		private class Holder
		{
			public PlainStruct Plain;
		}
	}
}
