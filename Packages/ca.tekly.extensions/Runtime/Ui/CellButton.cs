using System;
using Tekly.Common.Ui;
using Tekly.TwoD.Cells;
using UnityEngine;

namespace Tekly.Extensions.Ui
{
	public class CellButton : ButtonBase
	{
		[SerializeField] private CellImage m_image;
		[SerializeField] private RectTransform m_content;
		[SerializeField] private float m_contentDownOffset;
		[SerializeField] private float m_contentDisabledOffset;
		
		[SerializeField] private Color m_disabledContentColor = Color.gray;
		[SerializeField] private CanvasRenderer[] m_disabledContent;

		protected override void DoStateTransition(ButtonState state, bool instant)
		{
			switch (state) {
				case ButtonState.Up:
					m_image.SetAnimation("up");
					m_image.FrameUpdate();
					m_content.offsetMax = new Vector2(m_content.offsetMax.x, 0);
					SetDisabledContentColor(Color.white);
					break;
				case ButtonState.Down:
					m_image.SetAnimation("down");
					m_image.FrameUpdate();
					m_content.offsetMax = new Vector2(m_content.offsetMax.x, m_contentDownOffset);
					SetDisabledContentColor(Color.white);
					break;
				case ButtonState.Disabled:
					m_image.SetAnimation("disabled");
					m_image.FrameUpdate();
					m_content.offsetMax = new Vector2(m_content.offsetMax.x, m_contentDisabledOffset);
					SetDisabledContentColor(m_disabledContentColor);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
		}

		private void SetDisabledContentColor(Color color)
		{
			if (m_disabledContent == null) {
				return;
			}
			
			foreach (var canvasRenderer in m_disabledContent) {
				canvasRenderer.SetColor(color);
			}
		}
	}
}