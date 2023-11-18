using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Tekly.Common.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Sheets.Processing
{
	/// <summary>
	/// Handles deserializing JSON that will populate a ScriptableObject.
	/// If the field is a UnityEngine.Object and the JSON contains a string there it will try to hook up that reference
	/// by loading it from the AssetDatabase 
	/// </summary>
	public static class JsonExt
	{
		private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings();

		static JsonExt()
		{
			s_settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
			s_settings.Converters.Add(new UnityObjectTypeConverter());
			s_settings.ContractResolver = new UnityContractResolver();
		}

		public static void PopulateObject(string value, object target)
		{
			JsonConvert.PopulateObject(value, target, s_settings);
		}
	}

	public class UnityContractResolver : DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var fieldInfos = new List<FieldInfo>();
			var currentType = type;
			while (currentType != null) {
				fieldInfos.AddRange(currentType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance |
				                                          BindingFlags.Public | BindingFlags.NonPublic));
				currentType = currentType.BaseType;
			}

			return fieldInfos
				.Distinct()
				.Where(x => x != null &&
				            !x.IsDefined(typeof(NonSerializedAttribute)) &&
				            (x.IsPublic || x.IsDefined(typeof(SerializeField)) || x.Name.StartsWith("m_")) &&
				            (x.FieldType.IsDefined(typeof(SerializableAttribute)) || x.FieldType.IsValueType ||
				             x.FieldType.IsArray || typeof(Object).IsAssignableFrom(x.FieldType)))
				.Select(f => {
					var prop = CreateProperty(f, memberSerialization);
					prop.Writable = true;
					prop.Readable = true;
					return prop;
				})
				.ToList();
		}
	}

	public class UnityObjectTypeConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var assetName = reader.Value.ToString();
			var asset = AssetDatabaseExt.FindAndLoadFirst(assetName, objectType);

			if (asset == null) {
				Debug.LogError($"Failed to find asset: [{assetName}]");
			}

			return asset;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(Object).IsAssignableFrom(objectType);
		}
	}
}