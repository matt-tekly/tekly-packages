using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Unity value types are written as fixed size Bytes payloads, so a reader can skip them without knowing
	/// their shape.
	/// </summary>
	public static class SerialConverterUnity
	{
		private const int FLOAT = sizeof(float);
		private const int INT = sizeof(int);

		public static void RegisterAll(SerialConverterDatabase database)
		{
			database.Register(new SerialConverterDelegate<Vector2>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 2); s.Write(v); },
				s => ExpectLength(s, FLOAT * 2) ? s.ReadVector2() : default));

			database.Register(new SerialConverterDelegate<Vector3>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 3); s.Write(v); },
				s => ExpectLength(s, FLOAT * 3) ? s.ReadVector3() : default));

			database.Register(new SerialConverterDelegate<Vector4>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 4); s.Write(v); },
				s => ExpectLength(s, FLOAT * 4) ? s.ReadVector4() : default));

			database.Register(new SerialConverterDelegate<Quaternion>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 4); s.Write(v); },
				s => ExpectLength(s, FLOAT * 4) ? s.ReadQuaternion() : default));

			database.Register(new SerialConverterDelegate<Vector2Int>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(INT * 2); s.Write(v.x); s.Write(v.y); },
				s => ExpectLength(s, INT * 2) ? new Vector2Int(s.ReadInt(), s.ReadInt()) : default));

			database.Register(new SerialConverterDelegate<Vector3Int>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(INT * 3); s.Write(v.x); s.Write(v.y); s.Write(v.z); },
				s => ExpectLength(s, INT * 3) ? new Vector3Int(s.ReadInt(), s.ReadInt(), s.ReadInt()) : default));

			database.Register(new SerialConverterDelegate<Color>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 4); s.Write(v.r); s.Write(v.g); s.Write(v.b); s.Write(v.a); },
				s => ExpectLength(s, FLOAT * 4) ? new Color(s.ReadFloat(), s.ReadFloat(), s.ReadFloat(), s.ReadFloat()) : default));

			database.Register(new SerialConverterDelegate<Color32>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(4); s.WriteByte(v.r); s.WriteByte(v.g); s.WriteByte(v.b); s.WriteByte(v.a); },
				s => ExpectLength(s, 4) ? new Color32(s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()) : default));

			database.Register(new SerialConverterDelegate<Rect>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 4); s.Write(v.x); s.Write(v.y); s.Write(v.width); s.Write(v.height); },
				s => ExpectLength(s, FLOAT * 4) ? new Rect(s.ReadFloat(), s.ReadFloat(), s.ReadFloat(), s.ReadFloat()) : default));

			database.Register(new SerialConverterDelegate<Bounds>(WireType.Bytes,
				(s, v) => { s.WriteVarUInt(FLOAT * 6); s.Write(v.center); s.Write(v.size); },
				s => ExpectLength(s, FLOAT * 6) ? new Bounds(s.ReadVector3(), s.ReadVector3()) : default));
		}

		/// <summary>
		/// Reads a Bytes payload's length. If it isn't the expected length the payload is skipped and false is
		/// returned so the caller can fall back to a default.
		/// </summary>
		internal static bool ExpectLength(TokenInputStream stream, int expected)
		{
			var length = (long) stream.ReadVarUInt();

			if (length == expected) {
				return true;
			}

			Debug.LogWarning($"[SuperSerial] Expected a {expected} byte payload but found {length}. Using default.");
			stream.SkipBytes(length);
			return false;
		}
	}
}
