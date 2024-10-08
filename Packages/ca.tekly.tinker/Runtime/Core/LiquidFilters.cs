#if UNITY_EDITOR && TINKER_ENABLED_EDITOR
#define TINKER_ENABLED
#endif

#if TINKER_ENABLED
using System.IO;
using DotLiquid;
using Newtonsoft.Json;

namespace Tekly.Tinker.Core
{
	[TinkerPreserve]
	public static class LiquidFilters
	{
		[TinkerPreserve]
		public static string Stringify(object obj)
		{
			if (obj is DropProxy p) {
				return p.GetProxiedObject().ToString();	
			}

			if (obj == null) {
				return "null";
			}
			
			return obj.ToString();
		}
		
		[TinkerPreserve]
		public static string Json(object obj)
		{
			if (obj == null) {
				return "null";
			}
			
			if (obj is DropProxy p) {
				obj = p.GetProxiedObject();
			}
			
			using var stringWriter = new StringWriter();
			using var jsonWriter = new JsonTextWriter(stringWriter);
			
			TinkerServer.Instance.Serializer.Serialize(jsonWriter, obj);

			return stringWriter.ToString();
		}
		
		[TinkerPreserve]
		public static string CurrentUrl(Context context, string input)
		{
			var possibleCurrentUrl = context["Tinker.Url"];

			if (possibleCurrentUrl is string currentUrl) {
				return currentUrl == input ? "active" : "";
			}

			return "";
		}
	}
}
#endif