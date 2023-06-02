using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
	public class HierarchyRootInjector : HierarchyInjector
	{
#if UNITY_EDITOR
		public override void FindInjectables()
		{
			if (!gameObject.scene.isLoaded) {
				return;
			}
			
			base.FindInjectables();

			var scratch = new List<MonoBehaviour>();
			var roots = gameObject.scene.GetRootGameObjects();
			
			foreach (var root in roots) {
				if (root == gameObject) {
					continue;
				}
				
				GetInjectables(m_behaviours, scratch, root.transform);
				GetChildren(root, m_behaviours, m_childInjectors, scratch);	
			}
		}
#endif
	}
}