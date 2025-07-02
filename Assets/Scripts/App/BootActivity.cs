using Tekly.Lofi.Core;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.App
{
	public class BootActivity : InjectableActivity
	{
		[SerializeField] private LofiClipBankDefinitionRef m_clipBankRef;
		
		protected override bool IsDoneLoading()
		{
			return !Lofi.Instance.IsLoading;
		}

		protected override void LoadingStarted()
		{
			Lofi.Instance.LoadBank(m_clipBankRef);
		}
	}
}