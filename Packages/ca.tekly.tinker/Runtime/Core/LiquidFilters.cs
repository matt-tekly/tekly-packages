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

			return obj.ToString();
		}
	}
}