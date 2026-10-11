using TMPro;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class LabelWidget : Widget
	{
		internal const string MONOSPACED = "monospace";
		
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

	public static class LabelWidgetFactory
	{
		public static LabelWidget Label(this ContainerWidget parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Label));
			instance.Text = text;

			return instance;
		}

		/// <summary>
		/// A label styled as a heading, for titling a group of widgets.
		/// </summary>
		public static LabelWidget Heading(this ContainerWidget parent, string text, WidgetPrefab<LabelWidget> prefab = default)
		{
			return parent.Label(text, prefab.Or(WidgetPrefabs.Heading));
		}
	}
}
