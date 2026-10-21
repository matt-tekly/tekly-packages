using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Generated;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests.Generated
{
	/// <summary>
	/// The source generator's output, exercised the same way the reflection converter is in the other fixtures.
	/// </summary>
	public class GeneratedTests
	{
		[Test]
		public void GeneratedTypesDoNotUseTheReflectionConverter()
		{
			var serializer = new SuperSerializer();

			Assert.IsInstanceOf<SerialConverterSuperSerialize>(serializer.GetConverter(typeof(GenValues)));
			Assert.IsInstanceOf<SerialConverterSuperSerialize>(serializer.GetConverter(typeof(GenStruct)));
			Assert.IsInstanceOf<ISuperSerialGenerated>(new GenValues());
		}

		[Test]
		public void EveryFieldKindRoundTrips()
		{
			var expected = GenValues.Create();
			var actual = Serial.RoundTripFresh(expected, SuperSerialRegistry_Tekly_SuperSerial_Tests.RegisterAll);

			Assert.AreEqual(expected.Flag, actual.Flag);
			Assert.AreEqual(expected.SByte, actual.SByte);
			Assert.AreEqual(expected.Byte, actual.Byte);
			Assert.AreEqual(expected.Short, actual.Short);
			Assert.AreEqual(expected.UShort, actual.UShort);
			Assert.AreEqual(expected.Integer, actual.Integer);
			Assert.AreEqual(expected.UInteger, actual.UInteger);
			Assert.AreEqual(expected.Long, actual.Long);
			Assert.AreEqual(expected.ULong, actual.ULong);
			Assert.AreEqual(expected.Char, actual.Char);
			Assert.AreEqual(expected.Float, actual.Float);
			Assert.AreEqual(expected.Double, actual.Double);
			Assert.AreEqual(expected.String, actual.String);
			Assert.AreEqual(expected.Date, actual.Date);
			Assert.AreEqual(expected.Rarity, actual.Rarity);
			Assert.AreEqual(expected.Flags, actual.Flags);
			Assert.AreEqual(expected.Vector2, actual.Vector2);
			Assert.AreEqual(expected.Vector3, actual.Vector3);
			Assert.AreEqual(expected.Quaternion, actual.Quaternion);
			Assert.AreEqual(expected.NullableInt, actual.NullableInt);
			Assert.AreEqual(expected.NullableEnum, actual.NullableEnum);
			Assert.AreEqual(expected.Inner.Value, actual.Inner.Value);
			Assert.AreEqual(expected.Inner.Label, actual.Inner.Label);
			Assert.AreEqual(expected.Inner.Position, actual.Inner.Position);
			Assert.AreEqual(expected.Inner.Rarity, actual.Inner.Rarity);
			Assert.AreEqual(expected.Child.Id, actual.Child.Id);
			Assert.AreEqual(expected.Child.Tag, actual.Child.Tag);
			Assert.AreEqual(expected.Decimal, actual.Decimal);
			Assert.AreEqual(expected.Guid, actual.Guid);
			Assert.AreEqual(expected.Span, actual.Span);
			Assert.AreEqual(expected.Vector4, actual.Vector4);
			Assert.AreEqual(expected.Color, actual.Color);
			Assert.AreEqual(expected.Pair, actual.Pair);
			CollectionAssert.AreEqual(expected.List, actual.List);
			CollectionAssert.AreEqual(expected.Array, actual.Array);
			CollectionAssert.AreEquivalent(expected.Map, actual.Map);
			CollectionAssert.AreEqual(expected.Bytes, actual.Bytes);
			Assert.AreEqual(expected.Boxed, actual.Boxed);
			Assert.IsInstanceOf<Square>(actual.Shape);
			Assert.AreEqual(2, ((Square) actual.Shape).Side);
			Assert.AreEqual(3, actual.Shapes.Count);
			Assert.AreEqual(3, ((Circle) actual.Shapes[0]).Radius);
			Assert.IsNull(actual.Shapes[1]);
			Assert.AreEqual(4, ((Square) actual.Shapes[2]).Side);
		}

		[Test]
		public void StructRoundTripsAtTheTopLevelAndAsAField()
		{
			var expected = new GenStruct { Value = 9, Label = "nine", Position = Vector3.up, Rarity = Rarity.Epic };

			var top = Serial.RoundTrip(expected);
			Assert.AreEqual(expected, top);

			var holder = Serial.RoundTrip(new StructHolder { Struct = expected });
			Assert.AreEqual(expected, holder.Struct);
		}

		[Test]
		public void NullFieldsOfEveryKind()
		{
			var actual = Serial.RoundTrip(new GenValues());

			Assert.IsNull(actual.String);
			Assert.IsNull(actual.NullableInt);
			Assert.IsNull(actual.NullableEnum);
			Assert.IsNull(actual.Child);
			Assert.IsNull(actual.Pair);
			Assert.IsNull(actual.List);
			Assert.IsNull(actual.Array);
			Assert.IsNull(actual.Map);
			Assert.IsNull(actual.Bytes);
			Assert.IsNull(actual.Boxed);
			Assert.IsNull(actual.Shape);
			Assert.IsNull(actual.Shapes);
		}

		[Test]
		public void NullFieldsClearAnExistingObject()
		{
			var existing = GenValues.Create();
			using var memory = Serial.Write(new GenValues());

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			Assert.IsNull(actual.String);
			Assert.IsNull(actual.NullableInt);
			Assert.IsNull(actual.Child);
			Assert.IsNull(actual.List);
			Assert.AreEqual(0, actual.Integer);
			Assert.AreEqual(Vector3.zero, actual.Vector3);
		}

		[Test]
		public void EmptyStringIsNotNull()
		{
			Assert.AreEqual("", Serial.RoundTrip(new GenValues { String = "" }).String);
		}

		[Test]
		public void RemovedFieldIsSkipped()
		{
			using var memory = Serial.Write(new GenV1 { Kept = 1, Removed = "gone" });
			Assert.AreEqual(1, Serial.Read<GenV2>(memory).Kept);
		}

		[Test]
		public void FieldChangedTypeIsSkipped()
		{
			using var memory = Serial.Write(new GenV1 { Kept = 1, TypeChanged = 2, OldName = 3 });
			var v2 = Serial.Read<GenV2>(memory, existing: new GenV2 { TypeChanged = "stale" });

			Assert.AreEqual(1, v2.Kept);
			Assert.IsNull(v2.TypeChanged, "a field the data can't fill is reset to its declared default, unlike the reflection converter which leaves it");
			Assert.AreEqual(3, v2.NewName);
		}

		[Test]
		public void RenamedFieldWithSerialNameKeepsItsData()
		{
			using var memory = Serial.Write(new GenV1 { OldName = 3 });
			Assert.AreEqual(3, Serial.Read<GenV2>(memory).NewName);

			using var memory2 = Serial.Write(new GenV2 { NewName = 9 });
			Assert.AreEqual(9, Serial.Read<GenV1>(memory2).OldName);
		}

		[Test]
		public void AddedFieldReadsBackAsItsInitializer()
		{
			using var memory = Serial.Write(new GenV1 { Kept = 1 });

			Assert.AreEqual(99, Serial.Read<GenV2>(memory).Added, "fresh object");
			Assert.AreEqual(99, Serial.Read(memory, existing: new GenV2 { Added = 5 }).Added, "existing object with a different value");
		}

		[Test]
		public void InitializersOfEveryKindAreTheDefault()
		{
			// Only the fields in the data are written, so everything else must come back as its initializer.
			using var memory = Serial.Write(new GenV1());
			var actual = Serial.Read(memory, existing: new GenInitializers { Health = 1, Name = "x", Speed = 0, Rarity = Rarity.Epic, Position = Vector3.zero, Optional = 0 });

			Assert.AreEqual(100, actual.Health);
			Assert.AreEqual("player", actual.Name);
			Assert.AreEqual(1.5f, actual.Speed);
			Assert.AreEqual(Rarity.Rare, actual.Rarity);
			Assert.AreEqual(Vector3.one, actual.Position);
			Assert.AreEqual(7, actual.Optional);
			CollectionAssert.AreEqual(new[] { 1 }, actual.List, "a non constant initializer is left alone");
		}

		[Test]
		public void UnknownFieldsOfEveryWireTypeAreSkipped()
		{
			using var memory = new MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				output.WriteWireType(WireType.Raw);

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

				output.BeginBlockField(11);
				output.Write("inside");
				output.EndBlock();

				output.WriteField(XXHash.Hash32(nameof(GenV1.Kept)), 42);
				output.WriteEndObject();
			}

			Assert.AreEqual(42, Serial.Read<GenV1>(memory).Kept);
		}

		[Test]
		public void GeneratedHashesMatchRuntimeHashes()
		{
			using var memory = Serial.Write(new GenV1 { Kept = 1, Removed = "r", TypeChanged = 2, OldName = 3 });
			memory.Position = 0;

			using var input = new TokenInputStream(memory, true);
			Assert.AreEqual(WireType.Raw, input.ReadWireType());

			var hashes = new List<uint>();
			for (var hash = input.ReadFieldHash(); hash != 0; hash = input.ReadFieldHash()) {
				hashes.Add(hash);
				input.SkipValue();
			}

			CollectionAssert.AreEqual(new[] {
				XXHash.Hash32(nameof(GenV1.Kept)),
				XXHash.Hash32(nameof(GenV1.Removed)),
				XXHash.Hash32(nameof(GenV1.TypeChanged)),
				XXHash.Hash32("stable"),
			}, hashes);
		}

		[Test]
		public void DataWrittenByReflectionReadsIntoAGeneratedType()
		{
			using var memory = Serial.Write(new Pair(4, "four"));
			var actual = Serial.Read<GenPair>(memory);

			Assert.AreEqual(4, actual.Number);
			Assert.AreEqual("four", actual.Text);
		}

		[Test]
		public void DataWrittenByReflectionReadsIntoAGeneratedField()
		{
			using var memory = Serial.Write(new ReflectionHolder { Pair = new Pair(4, "four"), After = 5 });
			var actual = Serial.Read<GeneratedHolder>(memory);

			Assert.AreEqual(4, actual.Pair.Number);
			Assert.AreEqual("four", actual.Pair.Text);
			Assert.AreEqual(5, actual.After);
		}

		[Test]
		public void PolymorphicFieldsReadOnAFreshSerializerAfterRegisterAll()
		{
			var expected = new GenDrawing {
				Main = new Square { Id = 1, Side = 2 },
				Shapes = new List<Shape> { new Circle { Id = 2, Radius = 3 }, new Square { Id = 3, Side = 4 } },
			};

			var actual = Serial.RoundTripFresh(expected, SuperSerialRegistry_Tekly_SuperSerial_Tests.RegisterAll);

			Assert.AreEqual(2, ((Square) actual.Main).Side);
			Assert.AreEqual(3, ((Circle) actual.Shapes[0]).Radius);
			Assert.AreEqual(4, ((Square) actual.Shapes[1]).Side);
		}

		[Test]
		public void UnregisteredSubclassThrowsAndNamesRegisterAll()
		{
			using var memory = Serial.Write(new GenDrawing { Main = new Circle { Id = 1 } });

			var reader = new SuperSerializer(new SuperSerializerSettings { AutoRegister = false });
			var exception = Assert.Throws<SuperSerialException>(() => Serial.Read<GenDrawing>(memory, reader));

			StringAssert.Contains("RegisterAll", exception.Message);
		}

		[Test]
		public void PolymorphicFieldsReadOnAFreshSerializerWithNoRegistration()
		{
			// The generated registry announced itself on domain reload, and new SuperSerializer() applied it.
			var expected = new GenDrawing { Main = new Square { Id = 1, Side = 2 } };
			var actual = Serial.RoundTripFresh(expected);

			Assert.AreEqual(2, ((Square) actual.Main).Side);
		}

		[Test]
		public void RegistryAnnouncesItself()
		{
			Assert.GreaterOrEqual(SuperSerialRegistries.Count, 1);

			// Adding it again is a no-op.
			var before = SuperSerialRegistries.Count;
			SuperSerialRegistry_Tekly_SuperSerial_Tests.Announce();
			Assert.AreEqual(before, SuperSerialRegistries.Count);
		}

		[Test]
		public void LateRegistriesAreAppliedBeforeFailingOnAnUnknownType()
		{
			using var memory = Serial.Write(new GenDrawing { Main = new Circle { Id = 1 } });

			// A serializer made before a registry appears picks it up when it meets a type it doesn't know.
			var reader = new SuperSerializer(new SuperSerializerSettings { AutoRegister = false });
			reader.Settings.AutoRegister = true;

			var actual = Serial.Read<GenDrawing>(memory, reader);
			Assert.IsInstanceOf<Circle>(actual.Main);
		}

		[Test]
		public void RegisterAllInstallsStructDelegatesAndFactories()
		{
			var serializer = new SuperSerializer();
			SuperSerialRegistry_Tekly_SuperSerial_Tests.RegisterAll(serializer);

			Assert.IsTrue(SuperSerializeStructs<GenStruct>.IsRegistered);
			Assert.IsTrue(serializer.Settings.Converters.TryGetFactory(typeof(GenChild), out var factory));
			Assert.IsInstanceOf<GenChild>(factory());

			var data = new[] { new GenStruct { Value = 1, Label = "a" }, new GenStruct { Value = 2, Label = "b" } };

			using var memory = new MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				for (var i = 0; i < data.Length; i++) {
					SuperSerializeStructs<GenStruct>.Write(ref data[i], output, serializer);
				}
			}

			memory.Position = 0;
			var result = new GenStruct[2];

			using (var input = new TokenInputStream(memory, true)) {
				for (var i = 0; i < result.Length; i++) {
					SuperSerializeStructs<GenStruct>.Read(ref result[i], input, serializer);
				}
			}

			CollectionAssert.AreEqual(data, result);
		}

		[Test]
		public void CreateForSerializationRunsInitializers()
		{
			// The serializer never sees the ObjectFactory for a generated class, so turning constructors off
			// changes nothing.
			using var memory = Serial.Write(new GenV1());
			var actual = Serial.Read<GenInitializers>(memory);

			Assert.AreEqual(100, actual.Health);
			Assert.AreEqual("player", actual.Name);
		}

		[Test]
		public void OmittedDefaultsWriteNothing()
		{
			using var memory = Serial.Write(new GenOmit());

			// Raw marker then the end of object hash: nothing else.
			CollectionAssert.AreEqual(new byte[] { (byte) WireType.Raw, 0, 0, 0, 0 }, Serial.Body(memory));
		}

		[Test]
		public void OmittedDefaultsRoundTripWhenChanged()
		{
			var expected = new GenOmit {
				Health = 5,
				Name = "n",
				Nickname = "nick",
				Rarity = Rarity.Epic,
				Position = Vector3.one,
				Speed = 2,
				Maybe = 3,
				List = new List<int> { 1 },
				Optional = new Pair(1, "a"),
				Child = new GenChild { Id = 2 },
			};

			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(5, actual.Health);
			Assert.AreEqual("n", actual.Name);
			Assert.AreEqual("nick", actual.Nickname);
			Assert.AreEqual(Rarity.Epic, actual.Rarity);
			Assert.AreEqual(Vector3.one, actual.Position);
			Assert.AreEqual(2, actual.Speed);
			Assert.AreEqual(3, actual.Maybe);
			CollectionAssert.AreEqual(new[] { 1 }, actual.List);
			Assert.AreEqual(new Pair(1, "a"), actual.Optional);
			Assert.AreEqual(2, actual.Child.Id);
		}

		[Test]
		public void OmittedFieldsReadBackAsInitializersOnAnExistingObject()
		{
			var existing = new GenOmit { Health = 5, Name = "n", Rarity = Rarity.Epic, Position = Vector3.one, Speed = 2, Optional = new Pair(1, "a") };
			using var memory = Serial.Write(new GenOmit());

			var actual = Serial.Read(memory, existing: existing);

			Assert.AreSame(existing, actual);
			Assert.AreEqual(100, actual.Health);
			Assert.AreEqual("", actual.Name);
			Assert.AreEqual(Rarity.Rare, actual.Rarity);
			Assert.AreEqual(Vector3.zero, actual.Position);
			Assert.AreEqual(0, actual.Speed);
			Assert.IsNull(actual.Optional);
		}

		[Test]
		public void OmittingAnEmptyStringCollapsesNullAndEmpty()
		{
			// Name's initializer is "", so a null Name is omitted and comes back as "".
			Assert.AreEqual("", Serial.RoundTrip(new GenOmit { Name = null }).Name);

			// Nickname's initializer is null, so an empty Nickname is omitted and comes back as null.
			Assert.IsNull(Serial.RoundTrip(new GenOmit { Nickname = "" }).Nickname);
		}

		[Test]
		public void OptionalClassFieldIsOmittedOnlyWhenNull()
		{
			using var memory = Serial.Write(new GenOmit { Optional = new Pair(0, null) });
			Assert.IsNotNull(Serial.Read<GenOmit>(memory).Optional);
		}

		[Test]
		public void EmptyListIsNotOmitted()
		{
			var actual = Serial.RoundTrip(new GenOmit { List = new List<int>() });
			Assert.IsNotNull(actual.List);
			Assert.AreEqual(0, actual.List.Count);
		}

		[Test]
		public void DerivedTypeWritesInheritedFields()
		{
			var expected = new GenDerived { A = 1, B = 2, Hidden = 3, Ignored = 4 };
			var actual = Serial.RoundTrip(expected);

			Assert.AreEqual(1, actual.A);
			Assert.AreEqual(2, actual.B);
			Assert.AreEqual(3, actual.Hidden);
			Assert.AreEqual(0, actual.Ignored);
		}

		[Test]
		public void DerivedTypeInABaseTypedField()
		{
			var actual = Serial.RoundTripFresh(new BaseHolder { Base = new GenDerived { A = 1, B = 2 } }, SuperSerialRegistry_Tekly_SuperSerial_Tests.RegisterAll);

			Assert.IsInstanceOf<GenDerived>(actual.Base);
			Assert.AreEqual(2, ((GenDerived) actual.Base).B);
		}

		[Test]
		public void NestedTypeInAPartialContainer()
		{
			Assert.AreEqual(5, Serial.RoundTrip(new GenOuter.GenNested { Value = 5 }).Value);
		}

		[Test]
		public void ExtraDataInANestedGeneratedBlockIsSkipped()
		{
			using var memory = Serial.Write(new WideHolder { Item = new GenStruct { Value = 1, Label = "l" }, After = 3 });
			var narrow = Serial.Read<NarrowHolder>(memory);

			Assert.AreEqual(1, narrow.Item.X);
			Assert.AreEqual(3, narrow.After);
		}

		private class StructHolder
		{
			public GenStruct Struct;
		}

		private class ReflectionHolder
		{
			public Pair Pair;
			public int After;
		}

		private class GeneratedHolder
		{
			public GenPair Pair;
			public int After;
		}

		private class BaseHolder
		{
			public GenBase Base;
		}

		private class WideHolder
		{
			public GenStruct Item;
			public int After;
		}

		private class NarrowHolder
		{
			// Same first field name as GenStruct, no others. Written as GenStruct, read as GenPoint.
			[SerialName("Item")] public NarrowPoint Item;
			public int After;
		}
	}

	[SuperSerializable]
	internal partial struct NarrowPoint
	{
		[SerialName("Value")] public int X;
	}
}
