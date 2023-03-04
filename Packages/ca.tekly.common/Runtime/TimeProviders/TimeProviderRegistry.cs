using Tekly.Common.Registrys;
using Tekly.Common.Utils;

namespace Tekly.Common.TimeProviders
{
	public class TimeProviderRegistry : SingletonRegistry<ITimeProvider, TimeProviderRegistry> { }
}