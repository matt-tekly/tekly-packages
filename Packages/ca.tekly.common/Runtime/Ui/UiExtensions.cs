using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
    public static class UiExtensions
    {
        /// <summary>
        /// A better alternative is likely to just the set CanvasRenderer's alpha
        /// </summary>
        public static void SetAlpha(this Image target, float alpha)
        {
            target.color = target.color.SetAlpha(alpha);
        }

        public static void SetAlpha(this Shadow target, float alpha)
        {
            target.effectColor = target.effectColor.SetAlpha(alpha);
        }

        public static Color SetAlpha(this Color target, float alpha)
        {
            target.a = alpha;
            return target;
        }

        public static void Expand(this RectTransform target)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.sizeDelta = Vector2.zero;
        }
    }
}