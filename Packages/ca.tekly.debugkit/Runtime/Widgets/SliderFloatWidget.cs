using System;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class SliderFloatWidget : Widget
	{
		private readonly Action<float> m_setValue;
		private readonly Func<float> m_getValue;
		
		private readonly Slider m_slider;
		
		public SliderFloatWidget(Container container, string labelText, float min, float max, Func<float> getValue, Action<float> setValue)
		{
			m_setValue = setValue;
			m_getValue = getValue;
			m_slider = new Slider(labelText, min, max);
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