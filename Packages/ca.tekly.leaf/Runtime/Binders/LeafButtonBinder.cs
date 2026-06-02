using System;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.Leaf.Elements;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Leaf.Binders
{
	public class LeafButtonBinder : BasicBinder<ButtonModel>
	{
		[Tooltip("If true activate will be called when clicked")]
		[SerializeField] private bool m_activateOnClick = true;
		[Tooltip("If true activate will be called when selected")]
		[SerializeField] private bool m_activateOnSelect;

		private ILeafButton m_button;

		private bool m_initialized;
		
		private void Initialize()
		{
			if (m_initialized) {
				return;
			}
			
			if (TryGetComponent(out m_button)) {
				m_button.OnSelected.AddListener(selected => {
					if (selected && m_activateOnSelect) {
						Activate();
					}
				});

				m_button.OnClicked.AddListener(() => {
					if (m_activateOnClick) {
						Activate();
					}
				});
			} else {
				TkLogger.Get<LeafButtonBinder>().ErrorContext("LeafButtonBinder has a null button", this);
			}

			m_initialized = true;
		}

		protected override IDisposable Subscribe(ButtonModel model)
		{
			return m_model.Interactable.Subscribe(BindBool);
		}

		private void BindBool(bool value)
		{
			Initialize();
			m_button.interactable = value;
		}

		private void Activate()
		{
			if (m_model != null) {
				m_model.Activate();
			} else {
				TkLogger.Get<LeafButtonBinder>().ErrorContext("LeafButtonBinder activated without a model", this);
			}
		}
	}
}