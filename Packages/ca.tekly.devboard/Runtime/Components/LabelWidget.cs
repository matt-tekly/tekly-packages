using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class LabelWidget : Widget
	{
		public TMP_Text TextComponent => m_textComponent;

		public string Text {
			get => m_textComponent.text;
			set => m_textComponent.text = value;
		}

		[SerializeField] private TMP_Text m_textComponent;
	}
}