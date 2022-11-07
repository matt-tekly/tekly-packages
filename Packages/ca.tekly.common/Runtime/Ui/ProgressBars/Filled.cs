using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    public abstract class Filled : MonoBehaviour
    {
        public abstract float Fill { get; set; }
    }

    public enum FillDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }
}