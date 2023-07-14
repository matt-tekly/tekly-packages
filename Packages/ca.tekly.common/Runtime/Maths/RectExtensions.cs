using UnityEngine;

namespace Tekly.Common.Maths
{
	public static class RectExtensions
	{
		public static Rect Shrink(this Rect rect, float amount)
		{
			rect.Set(rect.xMin + amount, rect.yMin + amount, rect.width - amount * 2f, rect.height - amount * 2f);
			return rect;
		}
		
		public static Rect FitTargetIntoContainer(Rect target, Rect container)
		{
			var scaleFactor = Mathf.Min(container.width / target.width, container.height / target.height);
			var scaledWidth = target.width * scaleFactor;
			var scaledHeight = target.height * scaleFactor;
			var offsetX = (container.width - scaledWidth) * 0.5f;
			var offsetY = (container.height - scaledHeight) * 0.5f;

			return new Rect(container.x + offsetX, container.y + offsetY, scaledWidth, scaledHeight);
		}
	}
}