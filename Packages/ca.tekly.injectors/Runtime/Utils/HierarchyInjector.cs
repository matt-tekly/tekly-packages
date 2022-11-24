using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
	public class HierarchyInjector : MonoBehaviour
	{
		public List<InjectableBehaviour> Behaviours;

		public void Inject(InjectorContainer container)
		{
			foreach (var behaviour in Behaviours) {
				container.Inject(behaviour);
			}

			foreach (var behaviour in Behaviours) {
				behaviour.Initialize();
			}
			
			foreach (var behaviour in Behaviours) {
				behaviour.PostInitialize();
			}
		}
	}
}