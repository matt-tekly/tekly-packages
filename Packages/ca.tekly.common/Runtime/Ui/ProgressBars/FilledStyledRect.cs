using Tekly.Common.Ui.Styled;
using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    /// <summary>
    /// Fills a StyledRect by setting its reveal amount, so a ProgressBar or FillAnimator can drive it.
    /// Set the rect's Reveal to the direction the bar should grow in. Changing the fill only writes the rect's
    /// values: nothing moves and no mesh or canvas is rebuilt.
    /// </summary>
    [RequireComponent(typeof(StyledRect))]
    public class FilledStyledRect : Filled
    {
        private StyledRect m_rect;

        private StyledRect Rect {
            get {
                if (m_rect == null) {
                    m_rect = GetComponent<StyledRect>();
                }

                return m_rect;
            }
        }

        protected override void SetFill(float fill)
        {
            Rect.RevealAmount = fill;
        }
    }
}
