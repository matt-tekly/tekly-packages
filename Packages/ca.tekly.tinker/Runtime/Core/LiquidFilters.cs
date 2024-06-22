using DotLiquid;

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