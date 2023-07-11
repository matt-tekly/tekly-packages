using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class CellRenderer : CellAnimator
	{
		[SerializeField] private SpriteRenderer m_renderer;
		
		public override Color Color { get; set; }

		protected override Sprite RenderedSprite {
			get => m_renderer.sprite;
			set => m_renderer.sprite = value;
		}
		
		public override bool Visible {
			get => m_renderer.enabled;
			set => m_renderer.enabled = value;
		}
		
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (m_renderer == null) {
				m_renderer = GetComponent<SpriteRenderer>();
			}
			
			base.OnValidate();
		}
#endif
	}
}