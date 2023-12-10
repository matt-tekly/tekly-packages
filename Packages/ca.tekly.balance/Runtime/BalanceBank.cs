using System.Collections.Generic;
using Tekly.Content;
using Tekly.Logging;

namespace Tekly.Balance
{
	public class BalanceBank
	{
		public string Label { get; }
		public bool IsLoading => !m_operation.IsDone;
		public int References { get; private set; }
		
		private IList<BalanceObject> m_objects;
		private readonly Dictionary<string, BalanceObject> m_objectMap;
		
		private readonly IContentOperation<IList<BalanceObject>> m_operation;
		
		private readonly string m_balanceLabel;
		private readonly IContentProvider m_contentProvider;
		private readonly TkLogger m_logger = TkLogger.Get<BalanceBank>();
		
		public BalanceBank(string label, Dictionary<string, BalanceObject> objectMap, IContentProvider contentProvider)
		{
			m_objectMap = objectMap;
			Label = label;
			
			m_operation = ContentProvider.Instance.LoadAssetsAsync<BalanceObject>(label);
			m_operation.Completed += OperationCompleted;

			References = 1;
		}

		private void OperationCompleted(IContentOperation<IList<BalanceObject>> operation)
		{
			m_logger.Debug("Loading BalanceBank started: [{balance}]", ("balance", m_balanceLabel));
			
			if (m_operation.HasError) {
				m_logger.Error("Failed to load Balance [{balance}]", ("balance", m_balanceLabel));
				Dispose();
				return;
			}

			m_objects = m_operation.Result;
			if (m_objects == null || m_objects.Count == 0) {
				m_logger.Error("Loaded BalanceBank [{balance}] but found no objects", ("balance", m_balanceLabel));
				Dispose();
				return;
			}

			foreach (var balanceObject in m_objects) {
				m_objectMap.Add(balanceObject.Id, balanceObject);
			}
			
			m_logger.Debug("Loading BalanceBank finished: [{balance}]", ("balance", m_balanceLabel));
		}

		public void Dispose()
		{
			if (m_objects != null) {
				foreach (var balanceObject in m_objects) {
					m_objectMap.Remove(balanceObject.Id);
				}
			}
			
			m_operation.Release();
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