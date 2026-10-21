using System;
using System.Reflection;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Converter for types that implement ISuperSerialize. Nested in another value the payload is a length
	/// prefixed Block so unknown data can still be skipped. At the top level of a stream it is written Raw.
	/// </summary>
	public class SerialConverterSuperSerialize : SerialConverter
	{
		private readonly Type m_type;
		private readonly Func<object> m_factory;
		private readonly bool m_generated;

		public override WireType WireType => WireType.Block;

		public SerialConverterSuperSerialize(SerialConverterDatabase database, Type type)
		{
			m_type = type;
			m_generated = typeof(ISuperSerialGenerated).IsAssignableFrom(type);

			if (!database.TryGetFactory(type, out m_factory)) {
				m_factory = FindGeneratedFactory(type);
			}
		}

		public override bool Accepts(WireType wireType)
		{
			return wireType == WireType.Block || wireType == WireType.Raw || m_generated && wireType == WireType.Object;
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			((ISuperSerialize) value).Write(stream, serializer);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var obj = existing ?? Create(serializer);
			((ISuperSerialize) obj).Read(stream, serializer);
			return obj;
		}

		private object Create(SuperSerializer serializer)
		{
			return m_factory != null ? m_factory() : serializer.CreateInstance(m_type);
		}

		/// <summary>
		/// A [SuperSerializable] class carries a generated CreateForSerialization. The generated registry hands it
		/// over directly; this is the fallback when RegisterAll wasn't called. The lookup happens once per type.
		/// </summary>
		private static Func<object> FindGeneratedFactory(Type type)
		{
			if (type.IsValueType) {
				return null;
			}

			var method = type.GetMethod("CreateForSerialization", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
			if (method == null || method.ReturnType != type) {
				return null;
			}

			return (Func<object>) Delegate.CreateDelegate(typeof(Func<object>), method);
		}
	}
}
