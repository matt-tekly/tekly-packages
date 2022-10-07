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
    }
}