using System;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class StringBinder : Binder
	{
		[SerializeField] private ModelRef m_key;
		[SerializeField] private TMP_Text m_text;

		private IDisposable m_disposable;
		private StringValueModel m_stringValueModel;

		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out m_stringValueModel)) {
				m_disposable?.Dispose();
				m_disposable = m_stringValueModel.Subscribe(BindString);
			}
		}

		private void BindString(string value)
		{
			if (m_stringValueModel.NeedsLocalization) {
				m_text.text = Localizer.Instance.Localize(value);	
			} else {
				m_text.text = value;
			}
		}

		public override void UnBind()
		{
			m_disposable?.Dispose();
			m_stringValueModel = null;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_text == null) {
				m_text = GetComponent<TMP_Text>();
			}
		}
#endif
	}
}