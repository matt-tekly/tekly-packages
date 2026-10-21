using System;
using System.Collections.Generic;
using System.Reflection;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Reflection based converter. Serializes public fields and private fields marked [SerializeField] or
	/// [SerialName], and skips [NonSerialized] fields. Fields are identified by a hash of their name.
	/// </summary>
	public class SerialConverterGeneric : SerialConverter
	{
		private readonly Type m_type;
		private readonly FieldData[] m_fields;
		private readonly Dictionary<uint, int> m_fieldIndices;

		public override WireType WireType => WireType.Object;

		public SerialConverterGeneric(SerialConverterDatabase database, Type type)
		{
			m_type = type;
			database.RegisterType(type);

			var fieldInfos = CollectFields(type);

			m_fields = new FieldData[fieldInfos.Count];
			m_fieldIndices = new Dictionary<uint, int>(fieldInfos.Count);

			for (var i = 0; i < fieldInfos.Count; i++) {
				var field = new FieldData(fieldInfos[i]);
				m_fields[i] = field;

				if (field.Hash == 0) {
					throw new SuperSerialException($"Field [{field.Name}] in [{type}] hashes to 0, which marks the end of an object. Give it a [SerialName].");
				}

				if (m_fieldIndices.TryGetValue(field.Hash, out var otherIndex)) {
					throw new SuperSerialException($"Field name hash collision in [{type}] between [{field.Name}] and [{m_fields[otherIndex].Name}]. Give one a [SerialName].");
				}

				m_fieldIndices.Add(field.Hash, i);
			}
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			for (var i = 0; i < m_fields.Length; i++) {
				ref var field = ref m_fields[i];
				stream.WriteFieldHash(field.Hash);
				serializer.WriteValue(stream, field.Type, field.GetValue(value));
			}

			stream.WriteEndObject();
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var obj = existing ?? serializer.CreateInstance(m_type);

			while (true) {
				var hash = stream.ReadFieldHash();
				if (hash == 0) {
					break;
				}

				var wireType = stream.ReadWireType();

				if (m_fieldIndices.TryGetValue(hash, out var index)) {
					m_fields[index].Read(serializer, stream, obj, wireType);
				} else {
					stream.Skip(wireType);
				}
			}

			return obj;
		}

		private static List<FieldInfo> CollectFields(Type type)
		{
			// Walk from the root base class down so private fields of base classes are included.
			var hierarchy = new List<Type>();
			for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
				hierarchy.Add(current);
			}

			hierarchy.Reverse();

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			var result = new List<FieldInfo>();

			foreach (var current in hierarchy) {
				foreach (var field in current.GetFields(flags)) {
					if (field.IsNotSerialized) {
						continue;
					}

					if (field.IsPublic || field.IsDefined(typeof(SerializeField)) || field.IsDefined(typeof(SerialNameAttribute))) {
						result.Add(field);
					}
				}
			}

			return result;
		}

		private struct FieldData
		{
			private readonly FieldInfo m_fieldInfo;
			private readonly object m_defaultValue;
			private readonly bool m_reuseExisting;

			public readonly uint Hash;
			public readonly Type Type;
			public string Name => m_fieldInfo.Name;

			public FieldData(FieldInfo fieldInfo)
			{
				m_fieldInfo = fieldInfo;
				Type = fieldInfo.FieldType;
				Hash = XXHash.Hash32(SerialNameAttribute.GetName(fieldInfo));

				var isNullable = !Type.IsValueType || Nullable.GetUnderlyingType(Type) != null;
				m_defaultValue = isNullable ? null : Activator.CreateInstance(Type);

				// Only hand the existing value back for types where that saves an allocation or preserves identity.
				m_reuseExisting = !Type.IsPrimitive && !Type.IsEnum && Type != typeof(string);
			}

			public object GetValue(object obj)
			{
				return m_fieldInfo.GetValue(obj);
			}

			public void Read(SuperSerializer serializer, TokenInputStream stream, object obj, WireType wireType)
			{
				if (wireType == WireType.Null) {
					m_fieldInfo.SetValue(obj, m_defaultValue);
					return;
				}

				var existing = m_reuseExisting ? m_fieldInfo.GetValue(obj) : null;

				if (serializer.TryReadPayload(stream, wireType, Type, existing, out var value)) {
					m_fieldInfo.SetValue(obj, value);
				}
			}
		}
	}
}
