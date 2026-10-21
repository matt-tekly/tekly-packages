using System;
using System.Collections.Generic;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Holds the converter for every type the serializer has met, and the name hash for every type that can
	/// appear polymorphically.
	/// </summary>
	public class SerialConverterDatabase
	{
		private readonly Dictionary<Type, SerialConverter> m_converters = new Dictionary<Type, SerialConverter>();
		private readonly Dictionary<Type, uint> m_typeHashes = new Dictionary<Type, uint>();
		private readonly Dictionary<uint, Type> m_typesByHash = new Dictionary<uint, Type>();

		public SerialConverterDatabase()
		{
			SerialConverterPrimitives.RegisterAll(this);
			SerialConverterUnity.RegisterAll(this);
		}

		public SerialConverter Get(Type type)
		{
			if (!m_converters.TryGetValue(type, out var converter)) {
				converter = CreateConverter(type);
				m_converters[type] = converter;
			}

			return converter;
		}

		public void Register(Type type, SerialConverter converter)
		{
			m_converters[type] = converter;
		}

		public void Register<T>(SerialConverter<T> converter)
		{
			m_converters[typeof(T)] = converter;
			RegisterType(typeof(T));
		}

		/// <summary>
		/// Registers a type so it can be read when it appears as a value whose declared type is a base class or
		/// interface. Types met while writing register themselves, and so do the declared field and element types
		/// of any converter that gets built, but a concrete subclass that only ever appears as a value has to be
		/// registered by the reading side before it loads. Reading an unregistered type throws.
		/// </summary>
		public uint RegisterType(Type type)
		{
			if (m_typeHashes.TryGetValue(type, out var hash)) {
				return hash;
			}

			var name = SerialNameAttribute.GetName(type);
			hash = XXHash.Hash32(name);

			if (m_typesByHash.TryGetValue(hash, out var other) && other != type) {
				throw new SuperSerialException($"Type name hash collision between [{type}] and [{other}]. Give one a [SerialName].");
			}

			m_typeHashes[type] = hash;
			m_typesByHash[hash] = type;

			return hash;
		}

		public bool TryGetType(uint hash, out Type type)
		{
			return m_typesByHash.TryGetValue(hash, out type);
		}

		private SerialConverter CreateConverter(Type type)
		{
			if (typeof(ISuperSerialize).IsAssignableFrom(type)) {
				return new SerialConverterSuperSerialize(type);
			}

			if (type.IsEnum) {
				return new SerialConverterEnum(type);
			}

			var underlying = Nullable.GetUnderlyingType(type);
			if (underlying != null) {
				return Get(underlying);
			}

			if (type == typeof(byte[])) {
				return new SerialConverterByteArray();
			}

			if (type.IsArray) {
				if (type.GetArrayRank() != 1) {
					throw new SuperSerialException($"Multidimensional arrays are not supported: {type}");
				}

				return new SerialConverterArray(this, type);
			}

			if (type.IsGenericType) {
				var definition = type.GetGenericTypeDefinition();

				if (definition == typeof(List<>)) {
					return new SerialConverterList(this, type);
				}

				if (definition == typeof(Dictionary<,>)) {
					return new SerialConverterDictionary(this, type);
				}
			}

			if (type.IsPointer || type.IsByRef || typeof(Delegate).IsAssignableFrom(type)) {
				throw new SuperSerialException($"Type cannot be serialized: {type}");
			}

			return new SerialConverterGeneric(this, type);
		}
	}
}
