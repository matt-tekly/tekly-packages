using System;
using Tekly.DebugKit.Utils;
using UnityEngine.UIElements;


namespace Tekly.DebugKit.Widgets
{
	public class ButtonWidget : Widget
	{
		private readonly Func<bool> m_isInteractable;
		private readonly Button m_button;

		public ButtonWidget(Container container, string name, string classNames, Action onClick, Func<bool> isInteractable = null)
		{
			m_isInteractable = isInteractable;
			m_button = new Button(onClick);
			m_button.text = name;
			
			m_button.AddToClassList("dk-button");
			m_button.AddClassNames(classNames);
			
			container.Root.Add(m_button);
			
			if (m_isInteractable != null) {
				var interactable = m_isInteractable.Invoke();
				m_button.SetEnabled(interactable);
			}
		}
		
		public ButtonWidget(Container container, string name, Action onClick, Func<bool> isInteractable = null)
			: this(container, name, null, onClick, isInteractable)
		{
			
		}
		
		public override void Update()
		{
			if (m_isInteractable != null) {
				var interactable = m_isInteractable.Invoke();
				m_button.SetEnabled(interactable);
			}
		}
		
		public override void AddClass(string className)
		{
			m_button.AddToClassList(className);
		}

		public override void RemoveClass(string className)
		{
			m_button.RemoveFromClassList(className);
		}
	}
}