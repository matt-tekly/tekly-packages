using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
	[Serializable]
	public struct RegisteredComponent
	{
		public bool UseId;
		public string Id;
		public Component Component;
	}
	
	public class HierarchyInjector : MonoBehaviour
	{
		[SerializeField] protected bool m_injectOnAwake;
		
		[SerializeField] protected List<MonoBehaviour> m_behaviours = new List<MonoBehaviour>();
		[SerializeField] protected List<HierarchyInjector> m_childInjectors = new List<HierarchyInjector>();

		[SerializeField] protected List<RegisteredComponent> m_componentsToRegister = new List<RegisteredComponent>();
		
		private void Awake()
		{
			if (m_injectOnAwake) {
				Inject(null);
			}	
		}
		
		public void Inject(InjectorContainer container)
		{
			if (m_componentsToRegister.Count > 0 || container == null) {
				container = new InjectorContainer(container, name);
				
				foreach (var behaviour in m_componentsToRegister) {
					if (behaviour.UseId) {
						container.Register(behaviour.Component.GetType(), behaviour.Component, behaviour.Id);
					} else {
						container.Register(behaviour.Component.GetType(), behaviour.Component);	
					}
				}
			}
			
			var providers = GetComponents<IInjectionProvider>();
			
			foreach (var provider in providers) {
				provider.Provide(container);
			}
			
			foreach (var behaviour in m_behaviours) {
				container.Inject(behaviour);
			}
			
			foreach (var behaviour in m_childInjectors) {
				behaviour.Inject(container);
			}
		}

#if UNITY_EDITOR
		public virtual void OnValidate()
		{
			FindInjectables();
		}
		
		public virtual void FindInjectables()
		{
			m_behaviours.Clear();
			m_childInjectors.Clear();

			var scratch = new List<MonoBehaviour>();
			
			GetInjectables(m_behaviours, scratch, transform);
			GetChildren(gameObject, m_behaviours, m_childInjectors, scratch);
		}

		protected static void GetChildren(GameObject gameObject, List<MonoBehaviour> behaviours, List<HierarchyInjector> injectors, List<MonoBehaviour> scratch)
		{
			var transform = gameObject.transform;
            
			for (var index = 0; index < transform.childCount; ++index) {
				var child = transform.GetChild(index);
				var childInjector = child.GetComponent<HierarchyInjector>();
                
				if (childInjector != null && !childInjector.m_injectOnAwake) {
					injectors.Add(childInjector);
					continue;
				}
                
				GetInjectables(behaviours, scratch, child);

				GetChildren(child.gameObject, behaviours, injectors, scratch);
			}
		}

		protected static void GetInjectables(List<MonoBehaviour> behaviours, List<MonoBehaviour> scratch, Component child)
		{
			child.GetComponents(scratch);

			foreach (var behaviour in scratch) {
				if (TypeUtils.IsInjectable(behaviour)) {
					behaviours.Add(behaviour);
				}
			}
		}
#endif
	}
}