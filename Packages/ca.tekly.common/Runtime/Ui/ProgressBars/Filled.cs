using Tekly.Common.Maths;
using UnityEngine;

namespace Tekly.Common.Ui.ProgressBars
{
    public abstract class Filled : MonoBehaviour
    {
        [Range(0, 1)] [SerializeField] private float m_fill;
        [SerializeField] private FloatRange m_fillRange;
        
        public float Fill {
            get => m_fill;
            set {
                if (Mathf.Approximately(m_fill, value)) {
                    return;
                }

                m_fill = value;
                SetFill(FillAdjusted);
            }
        }
        
        public float FillAdjusted => m_fillRange.Lerp(m_fill);

        protected abstract void SetFill(float fill);
       
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_fillRange.Min == 0 && m_fillRange.Max == 0) {
                m_fillRange.Min = 0;
                m_fillRange.Max = 1f;
            }
            
            SetFill(FillAdjusted);
        }
#endif
    }

    public enum FillDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }
}