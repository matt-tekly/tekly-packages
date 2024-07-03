using System.IO;
using DotLiquid;
using Newtonsoft.Json;

namespace Tekly.Tinker.Core
{
	public static class LiquidFilters
	{
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