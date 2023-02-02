using UnityEngine;

namespace Tekly.Common.Utils
{
	public static class ColorExtensions
	{
		public static Color RGBMultiplied(this Color color, float multiplier)
		{
			return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);	
		}
		
		public static Color AlphaMultiplied(this Color color, float multiplier)
		{
			return new Color(color.r, color.g, color.b, color.a * multiplier);	
		}
	}
}