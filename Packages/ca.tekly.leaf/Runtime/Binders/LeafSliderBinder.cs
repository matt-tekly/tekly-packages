using Tekly.Common.Utils;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.Leaf.Binders
{
	public class LeafSliderBinder : Binder
	{
		[SerializeField] private LeafSlider m_slider;
		[SerializeField] private ModelRef m_key;

		private readonly Disposables m_disposables = new Disposables();

		private NumberValueModel m_numberModel;
		private RangeModel m_rangeModel;

		private void Awake()
		{
			m_slider.onValueChanged.AddListener(SliderChanged);
		}

		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out IModel model)) {
				if (model is NumberValueModel numberModel) {
					m_numberModel = numberModel;
					m_numberModel.SubscribeFloat(m_slider.SetValueWithoutNotify)
						.AddTo(m_disposables);
				} else if (model is RangeModel rangeModel) {
					m_rangeModel = rangeModel;
					
					m_rangeModel.Min.SubscribeFloat(value => m_slider.minValue = value)
						.AddTo(m_disposables);

					m_rangeModel.Max.SubscribeFloat(value => m_slider.maxValue = value)
						.AddTo(m_disposables);
					
					m_rangeModel.Current.SubscribeFloat(m_slider.SetValueWithoutNotify)
						.AddTo(m_disposables);
				} else {
					Debug.LogError($"LeafSliderBinder binding to unsupported model type [{model?.GetType()?.Name}]", this);
				}
			}
		}

		public override void UnBind()
		{
			m_disposables.Dispose();
			m_numberModel = null;
			m_rangeModel = null;
		}

		private void SliderChanged(float value)
		{
			if (m_numberModel != null) {
				m_numberModel.Value = value;
			} else if (m_rangeModel != null) {
				m_rangeModel.Current.Value = value;
			} else {
				Debug.LogError("LeafSliderBinder value changed without bound model", this);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_slider == null) {
				m_slider = GetComponent<LeafSlider>();
			}
		}
#endif
	}
}