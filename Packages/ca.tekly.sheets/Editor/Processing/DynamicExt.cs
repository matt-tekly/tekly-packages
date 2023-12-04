using System;
using Tekly.Common.Utils;
using Tekly.Sheets.Dynamics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Sheets.Processing
{
	public static class DynamicExt
	{
		private static readonly DynamicSerializer s_serializer = new DynamicSerializer();

		static DynamicExt()
		{
			s_serializer.Converters.Register(new DynamicConverterUnityAsset());
		}

		public static void PopulateObject(Dynamic dynamic, object target)
		{
			s_serializer.Populate(dynamic, target);
		}
	}
	
	public class DynamicConverterUnityAsset : DynamicConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(Object).IsAssignableFrom(objectType);
		}

		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			var assetName = dyn.ToString();
			
			if (existing is Object obj && obj != null && obj.name == assetName) {
				return existing;
			}
			
			var asset = AssetDatabaseExt.FindAndLoadFirst(assetName, type);

			if (asset == null) {
				Debug.LogError($"Failed to find asset: [{assetName}]");
			}

			return asset;
		}
	}
}