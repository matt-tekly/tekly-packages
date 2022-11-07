using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProgressBars
{
    
    public class FilledImage : Filled
    {
        [Range(0, 1)] [SerializeField] private float m_fill;
        
        private Image m_image;

        private Image Image {
            get {
                if (m_image == null) {
                    m_image = GetComponent<Image>();
                }

                return m_image;
            }
        }
        
        public override float Fill {
            get => m_fill;
            set {
                if (Mathf.Approximately(m_fill, value)) {
                    return;
                }

                m_fill = value;
                Image.fillAmount = Fill;
            }
        }

        private void OnValidate()
        {
            Image.fillAmount = Fill;
        }
    }
}