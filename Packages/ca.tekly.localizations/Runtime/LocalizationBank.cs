using System;
using Tekly.Content;
using Tekly.Logging;

namespace Tekly.Localizations
{
	public class LocalizationBank : IDisposable
	{
		public string Key { get; }
		
		public bool IsLoading => !m_operation.IsDone;
		public int References { get; private set; }
		
		private LocalizationData m_data;
		private readonly IContentOperation<LocalizationData> m_operation;
		private readonly Localizer m_localizer;
		private readonly TkLogger m_logger = TkLogger.Get<LocalizationBank>();

		public LocalizationBank(string key, Localizer localizer)
		{
			Key = key;
			m_localizer = localizer;

			if (string.IsNullOrEmpty(m_localizer.LanguageLabel)) {
				m_operation = ContentProvider.Instance.LoadAssetAsync<LocalizationData>(key);
			} else {
				m_operation = ContentProvider.Instance.LoadAssetAsync<LocalizationData>(key, localizer.LanguageLabel);	
			}
			
			m_operation.Completed += OperationCompleted;

			References = 1;
		}
		
		public void Dispose()
		{
			m_operation.Release();
		}
		
		private void OperationCompleted(IContentOperation<LocalizationData> operation)
		{
			if (m_operation.HasError) {
				m_logger.Exception(operation.Exception, "Failed to load LocalizationBank [{key}]", ("key", Key));
			} else {
				m_data = m_operation.Result;
				m_localizer.AddData(m_data);
			}
		}
		
		public void AddRef()
		{
			References++;
		}

		public void RemoveRef()
		{
			References--;
		}
	}
}