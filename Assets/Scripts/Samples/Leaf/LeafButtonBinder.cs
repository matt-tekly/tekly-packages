using System;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.Leaf.Elements;
using Tekly.Logging;
using UnityEngine;

namespace TeklySample.Samples.Leaf
{
	public class LeafButtonBinder : BasicBinder<ButtonModel>
	{
		[SerializeField] private LeafButton m_button;
		
		[Tooltip("If true activate will be called when clicked")]
		[SerializeField] private bool m_activateOnClick = true;
		[Tooltip("If true activate will be called when selected")]
		[SerializeField] private bool m_activateOnSelect;

		private void Awake()
		{
			if (m_button != null) {
				m_button.OnSelected.AddListener(selected => {
					if (selected && m_activateOnSelect) {
						Activate();
					}
				});

				m_button.OnClick.AddListener(() => {
					if (m_activateOnClick) {
						Activate();
					}
				});
			} else {
				TkLogger.Get<UnityButtonBinder>().ErrorContext("LeafButtonBinder has a null button", this);
			}
		}

		protected override IDisposable Subscribe(ButtonModel model)
		{
			return m_model.Interactable.Subscribe(BindBool);
		}

		private void BindBool(bool value)
		{
			m_button.interactable = value;
		}

		private void Activate()
		{
			Debug.Log(name + ": Activate");
			if (m_model != null) {
				m_model.Activate();
			} else {
				TkLogger.Get<UnityButtonBinder>().ErrorContext("LeafButtonBinder activated without a model", this);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_button == null) {
				m_button = GetComponent<LeafButton>();
			}
		}
#endif
	}
}