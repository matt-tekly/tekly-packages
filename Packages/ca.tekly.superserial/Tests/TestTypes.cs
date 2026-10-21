using System;
using System.Collections.Generic;
using System.Text;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	public enum Rarity { Common, Rare, Epic }

	public enum Flags64 : ulong { None = 0, Huge = 1UL << 63 }

	/// <summary>
	/// One field of every supported value type. ToString lists them so a failed comparison names the field.
	/// </summary>
	public class ValueTypes : IEquatable<ValueTypes>
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
		public decimal Decimal;
		public string String;
		public DateTime Date;
		public TimeSpan Span;
		public Guid Guid;
		public Rarity Rarity;
		public Flags64 Flags;
		public int? NullableInt;
		public Rarity? NullableEnum;

		public Vector2 Vector2;
		public Vector3 Vector3;
		public Vector4 Vector4;
		public Quaternion Quaternion;
		public Vector2Int Vector2Int;
		public Vector3Int Vector3Int;
		public Color Color;
		public Color32 Color32;
		public Rect Rect;
		public Bounds Bounds;

		public static ValueTypes Create(int seed = 1) => new ValueTypes {
			Flag = true,
			SByte = -8,
			Byte = 200,
			Short = -300,
			UShort = 60000,
			Integer = -123456 * seed,
			UInteger = 4000000000,
			Long = long.MinValue,
			ULong = ulong.MaxValue,
			Char = 'ß',
			Float = 3.1415f,
			Double = 1.2345d,
			Decimal = 1234.5678m,
			String = "STRING " + seed,
			Date = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
			Span = TimeSpan.FromMinutes(90),
			Guid = new Guid(seed, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
			Rarity = Rarity.Epic,
			Flags = Flags64.Huge,
			NullableInt = 77,
			NullableEnum = Rarity.Rare,
			Vector2 = new Vector2(1, 2),
			Vector3 = new Vector3(1, 2, 3),
			Vector4 = new Vector4(1, 2, 3, 4),
			Quaternion = new Quaternion(0, 1, 2, 3),
			Vector2Int = new Vector2Int(5, 6),
			Vector3Int = new Vector3Int(7, 8, 9),
			Color = new Color(0.1f, 0.2f, 0.3f, 0.4f),
			Color32 = new Color32(10, 20, 30, 40),
			Rect = new Rect(1, 2, 3, 4),
			Bounds = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6)),
		};

		public bool Equals(ValueTypes other)
		{
			return other != null && ToString() == other.ToString();
		}

		public override bool Equals(object obj) => Equals(obj as ValueTypes);
		public override int GetHashCode() => Integer;

		public override string ToString()
		{
			var builder = new StringBuilder();

			foreach (var field in typeof(ValueTypes).GetFields()) {
				builder.Append(field.Name).Append('=').Append(field.GetValue(this)).Append(' ');
			}

			return builder.ToString();
		}
	}

	public class Pair
	{
		public int Number;
		public string Text;

		public Pair() { }

		public Pair(int number, string text)
		{
			Number = number;
			Text = text;
		}

		public override bool Equals(object obj) => obj is Pair other && Number == other.Number && Text == other.Text;
		public override int GetHashCode() => Number;
		public override string ToString() => $"Pair({Number}, {Text})";
	}

	public struct PlainStruct
	{
		public int A;
		public string B;
		public Vector2 C;
	}

	public struct Point
	{
		public int X;
		public int Y;

		public Point(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	public abstract class Shape
	{
		public int Id;
	}

	public class Circle : Shape
	{
		public float Radius;
	}

	[SerialName("Tests.Square")]
	public class Square : Shape
	{
		public float Side;
	}

	public class BaseObject : ISuperSerialize
	{
		public string Name;

		public virtual void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			output.Write(Name);
		}

		public virtual void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			Name = input.ReadString();
		}
	}

	public class SubObject : BaseObject
	{
		public int Number;
		public Vector3 Vector3;

		public override void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			base.Write(output, superSerializer);
			output.Write(Number);
			output.Write(Vector3);
		}

		public override void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			base.Read(input, superSerializer);
			Number = input.ReadInt();
			input.Read(ref Vector3);
		}
	}

	public struct SerialStruct : ISuperSerialize
	{
		public int Value;
		public string Label;

		public void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			output.Write(Value);
			output.Write(Label);
		}

		public void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			Value = input.ReadInt();
			Label = input.ReadString();
		}
	}
}
