using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Reads and writes the payload of one type. The wire type byte and any field hash are written by the
	/// serializer, so a converter only ever sees its own payload.
	/// </summary>
	public abstract class SerialConverter
	{
		/// <summary>
		/// The wire type this converter's payload is written with.
		/// </summary>
		public abstract WireType WireType { get; }

		public abstract void Write(SuperSerializer serializer, TokenOutputStream stream, object value);
		public abstract object Read(SuperSerializer serializer, TokenInputStream stream, object existing);

		/// <summary>
		/// Whether this converter can read a payload that was written with the given wire type.
		/// </summary>
		public virtual bool Accepts(WireType wireType)
		{
			return wireType == WireType;
		}
	}

	/// <summary>
	/// Typed base for custom converters.
	/// </summary>
	public abstract class SerialConverter<T> : SerialConverter
	{
		public sealed override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			Write(serializer, stream, (T) value);
		}

		public sealed override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			return Read(serializer, stream, existing is T typed ? typed : default);
		}

		public abstract void Write(SuperSerializer serializer, TokenOutputStream stream, T value);
		public abstract T Read(SuperSerializer serializer, TokenInputStream stream, T existing);
	}

	/// <summary>
	/// A converter for a value that doesn't need the serializer, built from a pair of delegates.
	/// </summary>
	public sealed class SerialConverterDelegate<T> : SerialConverter<T>
	{
		private readonly WireType m_wireType;
		private readonly Action<TokenOutputStream, T> m_write;
		private readonly Func<TokenInputStream, T> m_read;

		public override WireType WireType => m_wireType;

		public SerialConverterDelegate(WireType wireType, Action<TokenOutputStream, T> write, Func<TokenInputStream, T> read)
		{
			m_wireType = wireType;
			m_write = write;
			m_read = read;
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, T value)
		{
			m_write(stream, value);
		}

		public override T Read(SuperSerializer serializer, TokenInputStream stream, T existing)
		{
			return m_read(stream);
		}
	}
}
