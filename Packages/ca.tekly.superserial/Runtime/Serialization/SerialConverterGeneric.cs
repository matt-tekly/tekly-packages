using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Tekly.SuperSerial.Streams;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace Tekly.SuperSerial.Serialization
{
	public abstract class SerialConverter
	{
		public abstract void Write(SuperSerializer serializer, TokenOutputStream stream, object obj);
		public abstract object Read(SuperSerializer serializer, TokenInputStream stream, object existing);
	}

	public class SerialConverterGeneric : SerialConverter
	{
		private readonly Type m_type;
		private readonly FieldData[] m_fields;
		private readonly int m_fieldsLength;

		private const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;

		private const uint END_FIELDS_HASH = 2974111186;

		public SerialConverterGeneric(Type type)
		{
			m_type = type;
			
			var fields = type.GetFields(FIELD_BINDING_FLAGS);
			m_fieldsLength = fields.Length;
			m_fields = new FieldData[m_fieldsLength];
			
			for (var i = 0; i < m_fieldsLength; i++) {
				m_fields[i] = new FieldData(fields[i]);
			}

			AssertNoCollisions();
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			for (var index = 0; index < m_fields.Length; index++) {
				var field = m_fields[index];
				field.Write(serializer, stream, obj);
			}

			stream.Write(END_FIELDS_HASH);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var obj = existing ?? serializer.CreateInstance(m_type);

			var iterations = 0;
			var fieldId = stream.ReadUInt();
			
			while (fieldId != END_FIELDS_HASH) {
				if (TryGetField(fieldId, out var fieldData)) {
					fieldData.Set(serializer, stream, obj);
				}

				fieldId = stream.ReadUInt();

				if (iterations++ > m_fieldsLength) {
					Debug.LogError("More iterations than fields. Something is wrong: " + m_type.Name);
					break;
				}
			}

			return obj;
		}

		private bool TryGetField(uint hash, out FieldData fieldData)
		{
			foreach (var field in m_fields) {
				if (field.Hash == hash) {
					fieldData = field;
					return true;
				}
			}

			throw new Exception("Unknown field: " + hash);
		}

		[Conditional("DEBUG")]
		private void AssertNoCollisions()
		{
			var hashSet = HashSetPool<uint>.Get();
			hashSet.Add(END_FIELDS_HASH);
			
			var failure = false;
			
			for (var index = 0; index < m_fieldsLength; index++) {
				var field = m_fields[index];
				if (!hashSet.Add(field.Hash)) {
					failure = true;
				}
			}

			if (failure) {
				var namesAndHash = m_fields.Select(x => $"[{x.Name}] = [{x.Hash}]");
				var message = $"Field Hash Collision in [{m_type.Name}]\n" + string.Join('\n', namesAndHash);
				Debug.LogError(message + $"\n[END_FIELDS_HASH] = [{END_FIELDS_HASH}]");
			}

			HashSetPool<uint>.Release(hashSet);
		}
	}

	public readonly struct FieldData
	{
		private readonly FieldInfo m_fieldInfo;
		private readonly Type m_fieldType;
		private readonly TypeCode m_typeCode;
		
		public readonly uint Hash;
		public string Name => m_fieldInfo.Name;

		public FieldData(FieldInfo fieldInfo)
		{
			m_fieldInfo = fieldInfo;
			m_fieldType = m_fieldInfo.FieldType;

			m_typeCode = Type.GetTypeCode(m_fieldType);
			
			// TODO: Check if the field has an attribute to override the hash
			Hash = XXHash.Hash32(m_fieldInfo.Name);
		}

		public void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			switch (m_typeCode) {
				case TypeCode.Boolean:
					stream.Write((bool) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.Double:
					stream.Write((double) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.Int32:
					stream.Write((int) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.Int64:
					stream.Write((long) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.Object:
					stream.Write(Hash);
					serializer.Write(stream, m_fieldInfo.GetValue(obj));
					break;
				case TypeCode.Single:
					stream.Write((float) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.String:
					stream.Write((string) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.DateTime:
					stream.Write((DateTime) m_fieldInfo.GetValue(obj), Hash);
					break;
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.DBNull:
				case TypeCode.Decimal:
				case TypeCode.Empty:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.SByte:
					throw new Exception("Unsupported type " + m_typeCode);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Set(SuperSerializer serializer, TokenInputStream stream, object obj)
		{
			switch (m_typeCode) {
				case TypeCode.Boolean:
					m_fieldInfo.SetValue(obj, stream.ReadBoolean());
					break;
				case TypeCode.Double:
					m_fieldInfo.SetValue(obj, stream.ReadDouble());
					break;
				case TypeCode.Int32:
					m_fieldInfo.SetValue(obj, stream.ReadInt());
					break;
				case TypeCode.Int64:
					m_fieldInfo.SetValue(obj, stream.ReadLong());
					break;
				case TypeCode.Object:
					m_fieldInfo.SetValue(obj, serializer.Read(stream, m_fieldType, null));
					break;
				case TypeCode.Single:
					m_fieldInfo.SetValue(obj, stream.ReadFloat());
					break;
				case TypeCode.String:
					m_fieldInfo.SetValue(obj, stream.ReadString());
					break;
				case TypeCode.DateTime:
					m_fieldInfo.SetValue(obj, stream.ReadDate());
					break;
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.DBNull:
				case TypeCode.Decimal:
				case TypeCode.Empty:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.SByte:
					throw new Exception("Unsupported type " + m_typeCode);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}