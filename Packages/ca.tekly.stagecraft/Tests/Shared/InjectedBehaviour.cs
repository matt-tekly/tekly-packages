using System;
using Tekly.Injectors;

namespace Tekly.Stagecraft.Tests
{
	public class InjectedBehaviour : ActorBehaviour
	{
		[Inject] [NonSerialized] public TestService Service;
	}
}
