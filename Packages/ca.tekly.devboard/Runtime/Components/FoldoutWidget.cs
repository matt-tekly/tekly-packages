using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class FoldoutWidget : ContainerWidget
	{
		public bool Expanded {
			get => m_content.gameObject.activeSelf;
			set => SetExpanded(value);
		}
		
		public ContainerWidget InfoContainer => m_infoContainer;

		[SerializeField] private ContainerWidget m_infoContainer;
		[SerializeField] private ButtonWidget m_button;

		private void Awake()
		{
			m_button.Initialize(Toggle, "Toggle");
		}

		public void Initialize(string label)
		{
			m_button.Label = label;
		}
		
		public void Toggle()
		{
			Expanded = !Expanded;
		}

		private void SetExpanded(bool expanded)
		{
			m_content.gameObject.SetActive(expanded);
		}
	}
}