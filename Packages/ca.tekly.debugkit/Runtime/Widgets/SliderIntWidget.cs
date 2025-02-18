using System;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class SliderIntWidget : Widget
	{
		private readonly Action<int> m_setValue;
		private readonly Func<int> m_getValue;
		
		private readonly SliderInt m_slider;
		
		public SliderIntWidget(Container container, string labelText, int min, int max, Func<int> getValue, Action<int> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			m_slider = new SliderInt(labelText, min, max);
			m_slider.AddToClassList("dk-slider");
			
			container.Root.Add(m_slider);

			m_slider.RegisterValueChangedCallback(evt => {
				m_setValue(evt.newValue);
			});
		}

		public override void Update()
		{
			m_slider.value = m_getValue();
		}
	}
}