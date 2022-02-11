// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.TreeState.Sequences;
using Tekly.TreeState.Utils;

namespace Tekly.TreeState
{
	public enum LoadType
	{
		Parallel,
		Sequential
	}
	
	[Serializable]
	public class TreeStateTransition
	{
		public string TransitionName;
		public TreeState Target;
	}

	/// <summary>
	/// A leaf state in a tree of states.
	/// TreeState controls a list of TreeStateActivitys that are attached to the same GameObject.
	/// When the TreeState starts loading or unloading so do the TreeStateActivitys it controls.
	///
	/// A TreeState is only considered completely loaded or unloaded if all the TreeStateActivitys it controls are also
	/// completely loaded or unloaded.
	///
	/// By default the TreeStateActivitys will be loaded or unloaded in parallel. You can change the LoadType so that
	/// they will load sequentially instead.
	/// </summary>
	public class TreeState : TreeActivity
	{
		public LoadType LoadType = LoadType.Parallel;

		public TreeStateTransition[] Transitions;

		public TreeStateMachine Machine { get; private set; }
		public TreeStateManager Manager { get; private set; }

		public override string FullName => m_fullName ??= TreeStateUtils.CalculatePath(this);

		private string m_fullName;
		
		private List<TreeStateActivity> m_activities;

		private ISequence m_loadingSequence;
		private ISequence m_unloadingSequence;
		
		public void Initialize(TreeStateManager manager)
		{
			Manager = manager;
		}
		
		public void TransitionTo(TreeState state)
		{
			Manager.TransitionTo(state);
		}
		
		public void HandleTransition(string transitionName)
		{
			Manager.HandleTransition(transitionName);
		}
		
		public bool TryGetTransition(string transitionName, out TreeState transition)
		{
			transition = null;
			
			if (Transitions == null) {
				return false;
			}

			foreach (var stateTransition in Transitions) {
				if (string.Equals(stateTransition.TransitionName, transitionName, StringComparison.Ordinal)) {
					transition = stateTransition.Target;
					return true;
				}
			}

			return false;
		}
		
		protected virtual void Awake()
		{
			Machine = UnityExtensions.GetComponentInDirectParent<TreeStateMachine>(this);
			m_activities = new List<TreeStateActivity>();
			GetComponents(m_activities);
		}
		
		protected override void LoadingStarted()
		{
			if (LoadType == LoadType.Parallel) {
				m_loadingSequence = new ParallelActivation<TreeStateActivity>(m_activities);	
			} else {
				m_loadingSequence = new SequencedActivation<TreeStateActivity>(m_activities, false);
			}
			
			m_loadingSequence.Begin();
		}

		protected override bool IsDoneLoading()
		{
			return m_loadingSequence.IsDone();
		}

		protected override void LoadingUpdate()
		{
			m_loadingSequence.Update();
		}

		protected override void UnloadingStarted()
		{
			if (LoadType == LoadType.Parallel) {
				m_unloadingSequence = new ParallelDeactivation<TreeStateActivity>(m_activities);	
			} else {
				var reversedActivities = m_activities.ToList();
				reversedActivities.Reverse();
				m_unloadingSequence = new SequencedDeactivation<TreeStateActivity>(reversedActivities);
			}
			
			m_unloadingSequence.Begin();
		}

		protected override bool IsDoneUnloading()
		{
			return m_unloadingSequence.IsDone();
		}

		protected override void UnloadingUpdate()
		{
			m_unloadingSequence.Update();
		}

		protected override void ActiveStarted()
		{
			foreach (var activity in m_activities) {
				activity.Activate();
			}
		}
	}
}