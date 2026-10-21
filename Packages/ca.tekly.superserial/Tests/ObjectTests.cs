// Fixture classes here are populated by the serializer, not by code, so the compiler can't see the assignments.
#pragma warning disable CS0649

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// How objects are created, which fields the reflection converter sees, and how existing instances are reused.
	/// </summary>
	public class ObjectTests
	{
		[Test]
		public void ConstructorsAreSkippedByDefault()
		{
			using var memory = Serial.Write(new WithConstructor { Initialized = 1, List = null });
			var actual = Serial.Read<WithConstructor>(memory);

			Assert.AreEqual(1, actual.Initialized);
			Assert.IsNull(actual.List, "the initializer did not run, and the data said null");
		}

		[Test]
		public void ConstructorsRunWhenAsked()
		{
			var serializer = new SuperSerializer();
			serializer.Settings.Factory.CallConstructors = true;

			using var memory = Serial.Write(new Empty(), serializer);
			var actual = Serial.Read<WithConstructor>(memory, serializer);

			Assert.AreEqual(5, actual.Initialized);
			Assert.IsNotNull(actual.List);
		}

		[Test]
		public void DataWinsOverTheConstructor()
		{
			var serializer = new SuperSerializer();
			serializer.Settings.Factory.CallConstructors = true;

			using var memory = Serial.Write(new WithConstructor { Initialized = 0 }, serializer);
			Assert.AreEqual(0, Serial.Read<WithConstructor>(memory, serializer).Initialized);
		}

		[Test]
		public void AbstractTypeCannotBeCreated()
		{
			using var memory = Serial.Write(new Empty());
			Assert.Throws<SuperSerialException>(() => Serial.Read<Shape>(memory));
		}

		[Test]
		public void PublicFieldsAreIncluded()
		{
			Assert.AreEqual(1, Serial.RoundTrip(new Visibility { Public = 1 }).Public);
		}

		[Test]
		public void NonSerializedFieldsAreSkipped()
		{
			Assert.AreEqual(0, Serial.RoundTrip(new Visibility { Ignored = 2 }).Ignored);
		}

		[Test]
		public void AutoPropertiesAreSkipped()
		{
			Assert.AreEqual(0, Serial.RoundTrip(new Visibility { Property = 3 }).Property);
		}

		[Test]
		public void PrivateFieldsNeedAnAttribute()
		{
			var expected = new Visibility();
			expected.Set(serialized: 4, bare: 5, renamed: 6);

			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual((4, 0, 6), actual.Get());
		}

		[Test]
		public void PrivateFieldsOfBaseClassesAreIncluded()
		{
			var expected = new Derived { Own = 1 };
			expected.SetBase(2);

			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(1, actual.Own);
			Assert.AreEqual(2, actual.GetBase());
		}

		[Test]
		public void FieldHashCollisionThrows()
		{
			var exception = Assert.Throws<SuperSerialException>(() => new SuperSerializer().GetConverter(typeof(Colliding)));
			StringAssert.Contains("collision", exception.Message);
		}

		[Test]
		public void ExistingObjectIsReused()
		{
			var existing = new Pair(9, "stale");
			using var memory = Serial.Write(new Pair(1, "a"));

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			Assert.AreEqual(new Pair(1, "a"), actual);
		}

		[Test]
		public void NestedObjectsKeepTheirIdentity()
		{
			var existing = new Outer { Inner = new Pair(), Items = new List<int>() };
			var inner = existing.Inner;
			var items = existing.Items;

			using var memory = Serial.Write(new Outer { Inner = new Pair(1, "a"), Items = new List<int> { 2 } });
			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(inner, actual.Inner);
			Assert.AreSame(items, actual.Items);
			Assert.AreEqual(new Pair(1, "a"), actual.Inner);
			CollectionAssert.AreEqual(new[] { 2 }, actual.Items);
		}

		[Test]
		public void NestedStructIsWrittenBack()
		{
			var expected = new Outer { Point = new Point(3, 4) };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(3, actual.Point.X);
			Assert.AreEqual(4, actual.Point.Y);
		}

		[Test]
		public void CustomConverterReplacesTheDefault()
		{
			var serializer = new SuperSerializer();
			serializer.RegisterConverter(new SerialConverterDelegate<Point>(WireType.Varint,
				(stream, value) => stream.WriteVarInt(value.X),
				stream => new Point((int) stream.ReadVarInt(), -1)));

			var actual = Serial.RoundTrip(new Outer { Point = new Point(5, 6) }, serializer);

			Assert.AreEqual(5, actual.Point.X);
			Assert.AreEqual(-1, actual.Point.Y);
		}

		[Test]
		public void CustomConverterCanBeTypedSubclass()
		{
			var serializer = new SuperSerializer();
			serializer.RegisterConverter(new PointAsString());

			var actual = Serial.RoundTrip(new Point(7, 8), serializer);

			Assert.AreEqual(7, actual.X);
			Assert.AreEqual(8, actual.Y);
		}

		private class PointAsString : SerialConverter<Point>
		{
			public override WireType WireType => WireType.Bytes;

			public override void Write(SuperSerializer serializer, TokenOutputStream stream, Point value)
			{
				stream.WriteUtf8($"{value.X},{value.Y}");
			}

			public override Point Read(SuperSerializer serializer, TokenInputStream stream, Point existing)
			{
				var parts = stream.ReadUtf8().Split(',');
				return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
			}
		}

		private class Empty { }

		private class WithConstructor
		{
			public int Initialized = 5;
			public List<int> List = new List<int>();
		}

		private class Visibility
		{
			public int Public;
			[SerializeField] private int m_serialized;
			private int m_bare;
			[NonSerialized] public int Ignored;
			public int Property { get; set; }
			[SerialName("renamed")] private int m_renamed;

			public void Set(int serialized, int bare, int renamed)
			{
				m_serialized = serialized;
				m_bare = bare;
				m_renamed = renamed;
			}

			public (int serialized, int bare, int renamed) Get() => (m_serialized, m_bare, m_renamed);
		}

		private class Base
		{
			[SerializeField] private int m_base;

			public void SetBase(int value) => m_base = value;
			public int GetBase() => m_base;
		}

		private class Derived : Base
		{
			public int Own;
		}

		private class Colliding
		{
			[SerialName("same")] public int A;
			[SerialName("same")] public int B;
		}

		private class Outer
		{
			public Pair Inner;
			public List<int> Items;
			public Point Point;
		}
	}
}
