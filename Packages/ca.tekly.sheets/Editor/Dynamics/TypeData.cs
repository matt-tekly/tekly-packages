using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tekly.Sheets.Dynamics
{
	public class TypeData
	{
		private readonly List<IMemberData> m_members = new List<IMemberData>();

		private const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;
		private const BindingFlags PROPERTY_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;

		public TypeData(Type type)
		{
			var fields = type.GetFields(FIELD_BINDING_FLAGS);
			foreach (var field in fields) {
				m_members.Add(new FieldData(field));
			}

			var properties = type.GetProperties(PROPERTY_BINDING_FLAGS);
			foreach (var field in properties) {
				if (field.CanWrite) {
					m_members.Add(new PropertyData(field));
				}
			}
		}

		public void Set(string name, DynamicSerializer serializer, object obj, object value)
		{
			if (TryGetField(name, out var memberData)) {
				memberData.Set(serializer, obj, value);
			} else {
				Debug.LogError($"Trying to set value but Failed to find member [{name}] of [{obj.GetType()}]");
			}
		}

		private bool TryGetField(string name, out IMemberData memberData)
		{
			foreach (var field in m_members) {
				if (field.Name == name) {
					memberData = field;
					return true;
				}
			}

			memberData = null;
			return false;
		}

		public static DateTime DateFromObject(object value)
		{
			if (value is double d) {
				return DateFromDouble(d);
			}

			if (value is long l) {
				return DateFromDouble(l);
			}

			return Convert.ToDateTime(value);
		}

		public static DateTime DateFromDouble(double days)
		{
			var epoch = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddDays(days);
		}
	}

	public interface IMemberData
	{
		public string Name { get; }

		void Set(DynamicSerializer serializer, object obj, object value);
	}

	public class FieldData : IMemberData
	{
		public string Name { get; }

		private readonly FieldInfo m_fieldInfo;
		private readonly Type m_targetType;
		private readonly TypeCode m_typeCode;

		public FieldData(FieldInfo fieldInfo)
		{
			Name = fieldInfo.Name;
			m_fieldInfo = fieldInfo;
			m_targetType = m_fieldInfo.FieldType;

			m_typeCode = Type.GetTypeCode(m_targetType);
		}

		public void Set(DynamicSerializer serializer, object obj, object value)
		{
			if (m_targetType.IsEnum) {
				m_fieldInfo.SetValue(obj, Enum.Parse(m_targetType, value.ToString()));
				return;
			}

			switch (m_typeCode) {
				case TypeCode.Boolean:
					m_fieldInfo.SetValue(obj, Convert.ToBoolean(value));
					break;
				case TypeCode.Double:
					m_fieldInfo.SetValue(obj, Convert.ToDouble(value));
					break;
				case TypeCode.Int32:
					m_fieldInfo.SetValue(obj, Convert.ToInt32(value));
					break;
				case TypeCode.Int64:
					m_fieldInfo.SetValue(obj, Convert.ToInt64(value));
					break;
				case TypeCode.Object:
					var existing = m_fieldInfo.GetValue(obj);
					m_fieldInfo.SetValue(obj, serializer.Deserialize(m_targetType, value, existing));
					break;
				case TypeCode.Single:
					m_fieldInfo.SetValue(obj, Convert.ToSingle(value));
					break;
				case TypeCode.String:
					m_fieldInfo.SetValue(obj, (string)value);
					break;
				case TypeCode.DateTime:
					m_fieldInfo.SetValue(obj, TypeData.DateFromObject(value));
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

	public class PropertyData : IMemberData
	{
		public string Name { get; }

		private readonly PropertyInfo m_propertyInfo;
		private readonly Type m_targetType;
		private readonly TypeCode m_typeCode;

		public PropertyData(PropertyInfo propertyInfo)
		{
			Name = propertyInfo.Name;
			m_propertyInfo = propertyInfo;
			m_targetType = m_propertyInfo.PropertyType;

			var underlyingType = Nullable.GetUnderlyingType(m_targetType);
			if (underlyingType != null) {
				m_targetType = underlyingType;
			}

			m_typeCode = Type.GetTypeCode(m_targetType);
		}

		public void Set(DynamicSerializer serializer, object obj, object value)
		{
			if (m_targetType.IsEnum) {
				m_propertyInfo.SetValue(obj, Enum.Parse(m_targetType, value.ToString()));
				return;
			}

			switch (m_typeCode) {
				case TypeCode.Boolean:
					m_propertyInfo.SetValue(obj, Convert.ToBoolean(value));
					break;
				case TypeCode.Double:
					m_propertyInfo.SetValue(obj, Convert.ToDouble(value));
					break;
				case TypeCode.Int32:
					m_propertyInfo.SetValue(obj, Convert.ToInt32(value));
					break;
				case TypeCode.Int64:
					m_propertyInfo.SetValue(obj, Convert.ToInt64(value));
					break;
				case TypeCode.Object:
					var existing = m_propertyInfo.GetValue(obj);
					m_propertyInfo.SetValue(obj, serializer.Deserialize(m_targetType, value, existing));
					break;
				case TypeCode.Single:
					m_propertyInfo.SetValue(obj, Convert.ToSingle(value));
					break;
				case TypeCode.String:
					m_propertyInfo.SetValue(obj, (string)value);
					break;
				case TypeCode.DateTime:
					m_propertyInfo.SetValue(obj, TypeData.DateFromObject(value));
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