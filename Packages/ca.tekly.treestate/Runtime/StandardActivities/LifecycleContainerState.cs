
using System;
using Tekly.Common.Utils;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
	public class LifecycleContainerState : TreeStateActivity, IInjectorContainerState
	{
		[Tooltip("The container will use this ref as the parent otherwise it will search up the hierarchy for a parent")]
		public InjectorContainerRef ParentContainer;

		[Tooltip("Marks the Parent Container as optional. It is not an error if it is not found.")]
		public bool ParentContainerIsOptional;

		[Tooltip("The container will initialize the ref with itself")]
		public InjectorContainerRef SelfContainer;

		public ScriptableBinding[] ScriptableBindings;

		public InjectorContainer Container { get; private set; }

		private InjectorContainer m_parentContainer;
		private ILifecycleInjectorProvider[] m_providers;
		private ScriptableBinding[] m_instances;
		private LifecycleContainer m_lifecycleContainer;

		protected override void Awake()
		{
			base.Awake();
			m_providers = GetComponents<ILifecycleInjectorProvider>();
		}

		protected override void PreLoad()
		{
			if (ParentContainer != null) {
				if (ParentContainerIsOptional) {
					ParentContainer.TryGetValue(out m_parentContainer);
				} else {
					m_parentContainer = ParentContainer.Value;
				}
			} else {
				var parent = transform.GetComponentInAncestor<IInjectorContainerState>();
				if (parent != null) {
					m_parentContainer = parent.Container;
				}
			}

			if (ScriptableBindings != null && ScriptableBindings.Length > 0 && (m_instances == null || m_instances.Length == 0)) {
				Array.Resize(ref m_instances, ScriptableBindings.Length);
				for (var index = 0; index < ScriptableBindings.Length; index++) {
					var scriptableInjector = ScriptableBindings[index];
					m_instances[index] = Instantiate(scriptableInjector);
				}
			}

			Container = new InjectorContainer(m_parentContainer, name);
			m_lifecycleContainer = new LifecycleContainer(Container);
			
			if (m_instances != null) {
				foreach (var scriptableInjector in m_instances) {
					Container.Inject(scriptableInjector);
					scriptableInjector.Bind(Container);
				}
			}

			foreach (var provider in m_providers) {
				provider.Provide(m_lifecycleContainer);
			}

			if (SelfContainer != null) {
				SelfContainer.Initialize(Container);
			}
		}

		protected override void LoadingStarted()
		{
			m_lifecycleContainer.Initialize();
		}

		protected override void LoadingUpdate()
		{
			m_lifecycleContainer.Tick();
		}
		
		protected override void ActiveUpdate()
		{
			m_lifecycleContainer.Tick();
		}
		
		protected override void UnloadingUpdate()
		{
			m_lifecycleContainer.Tick();
		}

		protected override void InactiveStarted()
		{
			if (m_instances != null) {
				foreach (var scriptableInjector in m_instances) {
					Container.Clear(scriptableInjector);
				}
			}

			Container = null;

			if (SelfContainer != null) {
				SelfContainer.Clear();
			}
			
			m_lifecycleContainer?.Dispose();
			m_lifecycleContainer = null;
		}
		
		protected override void OnApplicationQuit()
		{
			if (SelfContainer != null) {
				SelfContainer.Clear();
			}
			
			m_lifecycleContainer?.Dispose();
			m_lifecycleContainer = null;
		}
	}
}