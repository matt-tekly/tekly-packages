using Tekly.Common.Utils;

namespace Tekly.Common.Poolables
{
	public class GlobalPool : SingletonFactory<PoolableManager, GlobalPool>
	{
		protected override PoolableManager Create()
		{
			return new PoolableManager();
		}
	}
}