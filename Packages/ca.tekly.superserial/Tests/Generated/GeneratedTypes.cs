// Fixture types for the source generator. They are internal rather than private so the generated registry can
// name them. Fields are assigned by the serializer, so the compiler can't see the writes.
#pragma warning disable CS0649

using System;
using System.Collections.Generic;
using Tekly.SuperSerial.Serialization;
using UnityEngine;

namespace Tekly.SuperSerial.Tests.Generated
{
	[SuperSerializable]
	internal partial struct GenStruct
	{
		public int Value;
		public string Label;
		public Vector3 Position;
		public Rarity Rarity;
	}

	[SuperSerializable]
	internal sealed partial class GenChild
	{
		public int Id;
		public string Tag;
	}

	/// <summary>
	/// One field per inline fast path plus one per boxed fallback kind.
	/// </summary>
	[SuperSerializable]
	internal partial class GenValues
	{
		public bool Flag;
		public sbyte SByte;
		public byte Byte;
		public short Short;
		public ushort UShort;
		public int Integer;
		public uint UInteger;
		public long Long;
		public ulong ULong;
		public char Char;
		public float Float;
		public double Double;
		public string String;
		public DateTime Date;
		public Rarity Rarity;
		public Flags64 Flags;
		public Vector2 Vector2;
		public Vector3 Vector3;
		public Quaternion Quaternion;
		public int? NullableInt;
		public Rarity? NullableEnum;
		public GenStruct Inner;
		public GenChild Child;

		// Boxed through WriteValue / ReadPayload
		public decimal Decimal;
		public Guid Guid;
		public TimeSpan Span;
		public Vector4 Vector4;
		public Color Color;
		public Pair Pair;
		public List<int> List;
		public string[] Array;
		public Dictionary<string, int> Map;
		public byte[] Bytes;
		public object Boxed;
		public Shape Shape;
		public List<Shape> Shapes;

		public static GenValues Create() => new GenValues {
			Flag = true,
			SByte = -8,
			Byte = 200,
			Short = -300,
			UShort = 60000,
			Integer = -123456,
			UInteger = 4000000000,
			Long = long.MinValue,
			ULong = ulong.MaxValue,
			Char = 'ß',
			Float = 3.1415f,
			Double = 1.2345d,
			String = "STRING",
			Date = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
			Rarity = Rarity.Epic,
			Flags = Flags64.Huge,
			Vector2 = new Vector2(1, 2),
			Vector3 = new Vector3(1, 2, 3),
			Quaternion = new Quaternion(0, 1, 2, 3),
			NullableInt = 77,
			NullableEnum = Rarity.Rare,
			Inner = new GenStruct { Value = 5, Label = "inner", Position = new Vector3(4, 5, 6), Rarity = Rarity.Rare },
			Child = new GenChild { Id = 9, Tag = "child" },
			Decimal = 1234.5678m,
			Guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
			Span = TimeSpan.FromMinutes(90),
			Vector4 = new Vector4(1, 2, 3, 4),
			Color = new Color(0.1f, 0.2f, 0.3f, 0.4f),
			Pair = new Pair(3, "three"),
			List = new List<int> { 3, 2, 1 },
			Array = new[] { "a", "b" },
			Map = new Dictionary<string, int> { { "x", 1 }, { "y", 2 } },
			Bytes = new byte[] { 1, 2, 3 },
			Boxed = 12,
			Shape = new Square { Id = 1, Side = 2 },
			Shapes = new List<Shape> { new Circle { Id = 2, Radius = 3 }, null, new Square { Id = 3, Side = 4 } },
		};
	}

	[SuperSerializable]
	internal partial class GenV1
	{
		public int Kept;
		public string Removed;
		public int TypeChanged;
		[SerialName("stable")] public int OldName;
	}

	[SuperSerializable]
	internal partial class GenV2
	{
		public int Kept;
		public int Added = 99;
		public string TypeChanged;
		[SerialName("stable")] public int NewName;
	}

	/// <summary>
	/// Same fields as Pair, so data written by the reflection converter can be read by generated code.
	/// </summary>
	[SuperSerializable]
	internal partial class GenPair
	{
		public int Number;
		public string Text;
	}

	[SuperSerializable]
	internal partial class GenDrawing
	{
		public Shape Main;
		public List<Shape> Shapes;
	}

	[SuperSerializable(OmitDefaults = true)]
	internal partial class GenOmit
	{
		public int Health = 100;
		public string Name = "";
		public string Nickname;
		public Rarity Rarity = Rarity.Rare;
		public Vector3 Position = Vector3.zero;
		public float Speed;
		public int? Maybe;
		[SerialOptional] public List<int> List;
		[SerialOptional] public Pair Optional;
		[SerialOptional] public GenChild Child;
	}

	[SuperSerializable]
	internal partial class GenInitializers
	{
		public int Health = 100;
		public string Name = "player";
		public float Speed = 1.5f;
		public Rarity Rarity = Rarity.Rare;
		public Vector3 Position = Vector3.one;
		public List<int> List = new List<int> { 1 };
		[SerialOptional] public int Optional = 7;
	}

	[SuperSerializable]
	internal partial class GenBase
	{
		public int A;
		[SerializeField] protected int m_hidden;
		[NonSerialized] public int Ignored;

		public int Hidden
		{
			get => m_hidden;
			set => m_hidden = value;
		}
	}

	[SuperSerializable]
	internal partial class GenDerived : GenBase
	{
		public int B;
	}

	[SuperSerializable]
	internal partial struct GenPoint
	{
		public int X;
		public int Y;
	}

	internal partial class GenOuter
	{
		[SuperSerializable]
		internal partial class GenNested
		{
			public int Value;
		}
	}
}
