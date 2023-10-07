using System;
using Newtonsoft.Json;
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
			s_settings.Converters.Add(new UnityObjectTypeConverter());
		}

		public static void PopulateObject(string value, object target)
		{
			JsonConvert.PopulateObject(value, target, s_settings);
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