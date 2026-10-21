// Fixture classes here are populated by the serializer, not by code, so the compiler can't see the assignments.
#pragma warning disable CS0649

using System;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// Data written by one version of a class and read by another. Each test names the one change it covers.
	/// Readers log a warning when they skip a field whose type changed; that's a side effect, not the contract.
	/// </summary>
	public class SchemaChangeTests
	{
		[Test]
		public void RemovedFieldIsSkipped()
		{
			using var memory = Serial.Write(new V1 { Kept = 1, Removed = "gone" });
			var v2 = Serial.Read<V2>(memory);

			Assert.AreEqual(1, v2.Kept);
		}

		[Test]
		public void AddedFieldKeepsItsDefault()
		{
			using var memory = Serial.Write(new V1 { Kept = 1 });
			var v2 = Serial.Read<V2>(memory);

			Assert.AreEqual(0, v2.Added);
		}

		[Test]
		public void AddedFieldKeepsItsInitializerWhenConstructorsRun()
		{
			var serializer = new SuperSerializer();
			serializer.Settings.Factory.CallConstructors = true;

			using var memory = Serial.Write(new V1 { Kept = 1 }, serializer);
			var v2 = Serial.Read<V2>(memory, serializer);

			Assert.AreEqual(99, v2.Added);
		}

		[Test]
		public void FieldChangedFromIntToStringIsSkipped()
		{
			using var memory = Serial.Write(new V1 { TypeChanged = 2 });
			var v2 = Serial.Read<V2>(memory, existing: new V2 { TypeChanged = "untouched" });

			Assert.AreEqual("untouched", v2.TypeChanged);
		}

		[Test]
		public void FieldChangedFromStringToIntIsSkipped()
		{
			using var memory = Serial.Write(new V2 { TypeChanged = "x" });
			var v1 = Serial.Read<V1>(memory, existing: new V1 { TypeChanged = 7 });

			Assert.AreEqual(7, v1.TypeChanged);
		}

		[Test]
		public void SkippedFieldDoesNotDisturbTheFieldsAfterIt()
		{
			using var memory = Serial.Write(new V1 { Kept = 1, Removed = "gone", TypeChanged = 2, OldName = 3 });
			var v2 = Serial.Read<V2>(memory);

			Assert.AreEqual(1, v2.Kept);
			Assert.AreEqual(3, v2.NewName);
		}

		[Test]
		public void RenamedFieldWithSerialNameKeepsItsData()
		{
			using var memory = Serial.Write(new V1 { OldName = 3 });
			Assert.AreEqual(3, Serial.Read<V2>(memory).NewName);

			using var memory2 = Serial.Write(new V2 { NewName = 9 });
			Assert.AreEqual(9, Serial.Read<V1>(memory2).OldName);
		}

		[Test]
		public void IntWidensToLong()
		{
			using var memory = Serial.Write(new V1 { Kept = int.MinValue });
			Assert.AreEqual(int.MinValue, Serial.Read<KeptAsLong>(memory).Kept);
		}

		[Test]
		public void IntBecomesEnum()
		{
			using var memory = Serial.Write(new V1 { Kept = (int) Rarity.Epic });
			Assert.AreEqual(Rarity.Epic, Serial.Read<KeptAsEnum>(memory).Kept);
		}

		[Test]
		public void FieldOrderDoesNotMatter()
		{
			using var memory = new System.IO.MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				output.WriteWireType(WireType.Object);
				output.WriteField(Hash("stable"), 3); // OldName's [SerialName]
				output.WriteField(Hash(nameof(V1.TypeChanged)), 2);
				output.WriteField(Hash(nameof(V1.Kept)), 1);
				output.WriteEndObject();
			}

			var v1 = Serial.Read<V1>(memory);

			Assert.AreEqual(1, v1.Kept);
			Assert.AreEqual(2, v1.TypeChanged);
			Assert.AreEqual(3, v1.OldName);
		}

		[Test]
		public void UnknownFieldsOfEveryWireTypeAreSkipped()
		{
			using var memory = new System.IO.MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				output.WriteWireType(WireType.Object);

				output.WriteNullField(1);
				output.WriteField(2, 123);
				output.WriteField(3, 1.5f);
				output.WriteField(4, 2.5);
				output.WriteField(5, "bytes");
				output.WriteField(6, new Vector3(1, 2, 3));

				output.WriteFieldHash(7);
				output.WriteWireType(WireType.Object);
				output.WriteField(70, 1);
				output.WriteEndObject();

				output.WriteFieldHash(8);
				output.WriteWireType(WireType.TypedObject);
				output.Write(0xDEADBEEF);
				output.WriteWireType(WireType.Varint);
				output.WriteVarInt(1);

				output.WriteFieldHash(9);
				output.WriteWireType(WireType.List);
				output.WriteVarUInt(2);
				output.WriteWireType(WireType.Varint);
				output.WriteVarInt(1);
				output.WriteWireType(WireType.Null);

				output.WriteFieldHash(10);
				output.WriteWireType(WireType.Map);
				output.WriteVarUInt(1);
				output.WriteWireType(WireType.Bytes);
				output.WriteUtf8("k");
				output.WriteWireType(WireType.Fixed64);
				output.Write(2.0);

				output.WriteFieldHash(11);
				output.WriteWireType(WireType.Block);
				output.BeginBlock();
				output.Write("inside");
				output.EndBlock();

				output.WriteField(Hash(nameof(V1.Kept)), 42);
				output.WriteEndObject();
			}

			Assert.AreEqual(42, Serial.Read<V1>(memory).Kept);
		}

		private static uint Hash(string name) => XXHash.Hash32(name);

		private class V1
		{
			public int Kept;
			public string Removed;
			public int TypeChanged;
			[SerialName("stable")] public int OldName;
		}

		private class V2
		{
			public int Kept;
			public int Added = 99;
			public string TypeChanged;
			[SerialName("stable")] public int NewName;
		}

		private class KeptAsLong
		{
			public long Kept;
		}

		private class KeptAsEnum
		{
			public Rarity Kept;
		}
	}
}
