using Tekly.Common.Registrys;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Common.TimeProviders
{
	[CreateAssetMenu(menuName = "Tekly/Time Provider/Ref")]
	public class TimeProviderRef : RegisterableRef<ITimeProvider>
	{
		public override IRegistry<ITimeProvider> Registry => TimeProviderRegistry.Instance;
	}
}