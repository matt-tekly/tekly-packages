using UnityEngine;
using UnityEngine.UI;

namespace Tekly.TwoD.Cells
{
	[RequireComponent(typeof(Image))]
	public class CellImage : CellAnimator
	{
		[HideInInspector][SerializeField] private Image m_image;
        
		public override Color Color {
			get => m_image.color;
			set => m_image.color = value;
		}
		
		protected override Sprite RenderedSprite {
			get => m_image.sprite;
			set => m_image.sprite = value;
		}

		public override bool Visible {
			get => m_image.enabled;
			set => m_image.enabled = value;
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (m_image == null) {
				m_image = GetComponent<Image>();
			}
			
			base.OnValidate();
		}
#endif
	}
}