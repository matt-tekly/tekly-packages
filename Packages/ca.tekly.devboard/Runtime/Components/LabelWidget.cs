using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class LabelWidget : Widget
	{
		private const string MONOSPACED = "monospace";
		
		public TMP_Text TextComponent => m_textComponent;

		public string Text {
			get => m_textComponent.text;
			set => m_textComponent.text = value;
		}

		public bool Monospaced {
			get => m_textComponent.textStyle == m_textComponent.styleSheet.GetStyle(MONOSPACED);
			set {
				if (value) {
					SetStyle(MONOSPACED);	
				} else {
					m_textComponent.textStyle = TMP_Style.NormalStyle;
				} 
			}
		}

		[SerializeField] private TMP_Text m_textComponent;

		public void SetStyle(string style)
		{
			m_textComponent.textStyle = m_textComponent.styleSheet.GetStyle(style);
		}
	}
}