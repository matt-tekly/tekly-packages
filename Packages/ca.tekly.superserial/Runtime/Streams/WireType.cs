namespace Tekly.SuperSerial.Streams
{
	/// <summary>
	/// Describes how a value's payload is laid out so a reader can skip values it doesn't understand.
	///
	/// A value is [WireType:u8][payload]. A field is [hash:u32][value]. An object payload is a sequence
	/// of fields terminated by a hash of 0.
	/// </summary>
	public enum WireType : byte
	{
		/// <summary>No payload.</summary>
		Null = 0,

		/// <summary>Unsigned LEB128 varint. Signed integers are zigzag encoded first.</summary>
		Varint = 1,

		/// <summary>4 bytes, little endian.</summary>
		Fixed32 = 2,

		/// <summary>8 bytes, little endian.</summary>
		Fixed64 = 3,

		/// <summary>[length:varint][bytes]</summary>
		Bytes = 4,

		/// <summary>Fields terminated by a zero hash.</summary>
		Object = 5,

		/// <summary>[typeHash:u32][value] - a value whose runtime type differs from its declared type.</summary>
		TypedObject = 6,

		/// <summary>[count:varint] followed by count values.</summary>
		List = 7,

		/// <summary>[count:varint] followed by count key/value pairs, each a value.</summary>
		Map = 8,

		/// <summary>[length:u32][bytes] - opaque, length prefixed. Used for nested ISuperSerialize values.</summary>
		Block = 9,

		/// <summary>
		/// Untagged bytes written directly by an ISuperSerialize implementation. Only valid at the top level
		/// of a stream, since it cannot be skipped.
		/// </summary>
		Raw = 10,
	}
}
