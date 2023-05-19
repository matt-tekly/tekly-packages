using UnityEngine.UI;

namespace Tekly.Common.Ui.ProgressBars
{
    public class FilledImage : Filled
    {
        private Image m_image;

        private Image Image {
            get {
                if (m_image == null) {
                    m_image = GetComponent<Image>();
                }

                return m_image;
            }
        }

        protected override void SetFill(float fill)
        {
            Image.fillAmount = fill;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Image.fillAmount = FillAdjusted;
        }
#endif
    }
}