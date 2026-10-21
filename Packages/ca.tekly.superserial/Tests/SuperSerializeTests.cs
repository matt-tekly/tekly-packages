using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// Types that write themselves through ISuperSerialize, on every path they can be reached by.
	/// </summary>
	public class SuperSerializeTests
	{
		[Test]
		public void ClassAtTheTopLevel()
		{
			var expected = new SubObject { Name = "TEST_A", Number = 11, Vector3 = new Vector3(1, 2, 3) };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(expected.Name, actual.Name);
			Assert.AreEqual(expected.Number, actual.Number);
			Assert.AreEqual(expected.Vector3, actual.Vector3);
		}

		[Test]
		public void StructAtTheTopLevel()
		{
			var actual = Serial.RoundTrip(new SerialStruct { Value = 9, Label = "nine" });

			Assert.AreEqual(9, actual.Value);
			Assert.AreEqual("nine", actual.Label);
		}

		[Test]
		public void StructAsAField()
		{
			var actual = Serial.RoundTrip(new Holder { Struct = new SerialStruct { Value = 9, Label = "nine" } });

			Assert.AreEqual(9, actual.Struct.Value);
			Assert.AreEqual("nine", actual.Struct.Label);
		}

		[Test]
		public void ClassAsAField()
		{
			var actual = Serial.RoundTrip(new Holder { Class = new SubObject { Name = "n", Number = 2 } });

			Assert.AreEqual("n", actual.Class.Name);
			Assert.AreEqual(2, actual.Class.Number);
		}

		[Test]
		public void StructThroughSuperSerializeStructs()
		{
			// The path Simulant's DataPool uses: ref access into an array, no boxing.
			var data = new[] { new SerialStruct { Value = 1, Label = "a" }, new SerialStruct { Value = 2, Label = "b" } };
			var serializer = new SuperSerializer();

			using var memory = new MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				var write = SuperSerializeStructs<SerialStruct>.Write;
				for (var i = 0; i < data.Length; i++) {
					write(ref data[i], output, serializer);
				}
			}

			memory.Position = 0;
			var result = new SerialStruct[2];

			using (var input = new TokenInputStream(memory, true)) {
				var read = SuperSerializeStructs<SerialStruct>.Read;
				for (var i = 0; i < result.Length; i++) {
					read(ref result[i], input, serializer);
				}
			}

			Assert.AreEqual(1, result[0].Value);
			Assert.AreEqual("a", result[0].Label);
			Assert.AreEqual(2, result[1].Value);
			Assert.AreEqual("b", result[1].Label);
		}

		[Test]
		public void SuperSerializeStructsRejectsOtherStructs()
		{
			Assert.Throws<System.TypeInitializationException>(() => { var _ = SuperSerializeStructs<Point>.Write; });
		}

		[Test]
		public void NestedValuesThroughTheSerializer()
		{
			var expected = new Composite {
				Sub = new SubObject { Name = "SUB_A", Number = 1111 },
				Numbers = new List<int> { 3, 2, 1 },
			};

			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(expected.Sub.Name, actual.Sub.Name);
			Assert.AreEqual(expected.Sub.Number, actual.Sub.Number);
			CollectionAssert.AreEqual(expected.Numbers, actual.Numbers);
		}

		[Test]
		public void NestedNullsThroughTheSerializer()
		{
			var actual = Serial.RoundTrip(new Composite { Sub = null, Numbers = null });

			Assert.IsNull(actual.Sub);
			Assert.IsNull(actual.Numbers);
		}

		[Test]
		public void NullStringsRoundTripRaw()
		{
			var actual = Serial.RoundTrip(new SubObject { Name = null });
			Assert.IsNull(actual.Name);
		}

		[Test]
		public void ReadsIntoAnExistingInstance()
		{
			var existing = new SubObject();
			using var memory = Serial.Write(new SubObject { Name = "N", Number = 2 });

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			Assert.AreEqual("N", actual.Name);
			Assert.AreEqual(2, actual.Number);
		}

		[Test]
		public void ExtraDataInANestedBlockIsSkipped()
		{
			// Written by a version that wrote two ints, read by one that reads only the first.
			using var memory = Serial.Write(new WideHolder { Item = new WideSerial { A = 1, B = 2 }, After = 3 });
			var narrow = Serial.Read<NarrowHolder>(memory);

			Assert.AreEqual(1, narrow.Item.A);
			Assert.AreEqual(3, narrow.After);
		}

		[Test]
		public void ReadingPastANestedBlockThrows()
		{
			using var memory = Serial.Write(new NarrowHolder { Item = new NarrowSerial { A = 1 }, After = 3 });
			Assert.Throws<SuperSerialException>(() => Serial.Read<WideHolder>(memory));
		}

		[Test]
		public void TopLevelRawCannotBeReadAsAnotherType()
		{
			using var memory = Serial.Write(new SubObject { Name = "n" });
			Assert.Throws<SuperSerialException>(() => Serial.Read<Pair>(memory));
		}

		private class Holder
		{
			public SerialStruct Struct;
			public SubObject Class;
		}

		private class Composite : ISuperSerialize
		{
			public SubObject Sub;
			public List<int> Numbers;

			public void Write(TokenOutputStream output, SuperSerializer superSerializer)
			{
				superSerializer.Write(output, Sub);
				superSerializer.Write(output, Numbers);
			}

			public void Read(TokenInputStream input, SuperSerializer superSerializer)
			{
				Sub = superSerializer.Read(input, Sub);
				Numbers = superSerializer.Read(input, Numbers);
			}
		}

		private class WideSerial : ISuperSerialize
		{
			public int A;
			public int B;

			public void Write(TokenOutputStream output, SuperSerializer superSerializer)
			{
				output.Write(A);
				output.Write(B);
			}

			public void Read(TokenInputStream input, SuperSerializer superSerializer)
			{
				A = input.ReadInt();
				B = input.ReadInt();
			}
		}

		private class NarrowSerial : ISuperSerialize
		{
			public int A;

			public void Write(TokenOutputStream output, SuperSerializer superSerializer) => output.Write(A);
			public void Read(TokenInputStream input, SuperSerializer superSerializer) => A = input.ReadInt();
		}

		private class WideHolder
		{
			public WideSerial Item;
			public int After;
		}

		private class NarrowHolder
		{
			[SerialName("Item")] public NarrowSerial Item;
			public int After;
		}
	}
}
