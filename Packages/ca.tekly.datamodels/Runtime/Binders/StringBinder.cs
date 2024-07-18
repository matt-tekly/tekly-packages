using System;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class StringBinder : Binder
	{
		public TextSetEvent OnTextSet => m_onTextSet;
		
		[SerializeField] private ModelRef m_key;
		[SerializeField] private TMP_Text m_text;
		[SerializeField] private TextSetEvent m_onTextSet = new TextSetEvent();

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
			string text;
			
			if (m_stringValueModel.NeedsLocalization) {
				text = Localizer.Instance.Localize(value);	
			} else {
				text = value;
			}

			if (m_text != null) {
				m_text.text = text;
			}
			
			m_onTextSet.Invoke(text);
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