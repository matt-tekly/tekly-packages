using Tekly.Common.Utils;

namespace Tekly.DataModels.Models
{
	public class DisposableObjectModel : ObjectModel
	{
		protected readonly Disposables m_disposables = new Disposables();
		
		protected override void OnDispose()
		{
			base.OnDispose();
			m_disposables.Dispose();
		}
	}
}