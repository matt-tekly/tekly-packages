using System;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace TeklySample.Samples.Leaf
{
	public class LeafSliderBinder : BasicBinder<NumberValueModel>
	{
		[SerializeField] private LeafSlider m_slider;

		private void Awake()
		{
			m_slider.onValueChanged.AddListener(SliderChanged);
		}

		private void SliderChanged(float value)
		{
			m_model.Value = value;
		}

		protected override IDisposable Subscribe(NumberValueModel model)
		{
			return model.Subscribe(value => {
				m_slider.SetValueWithoutNotify((float)value);
			});
		}
	}
}