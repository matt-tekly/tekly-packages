// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using Tekly.Logging;
using Tekly.TreeState.Sequences;
using Tekly.TreeState.Utils;
using UnityEngine;

namespace Tekly.TreeState
{
	public class TreeStateManager : MonoBehaviour, IActivityMonitor, ISequenceMonitor
	{
		[NonSerialized] public TreeState Active;
		
		public TreeStateMachine StateMachine;

		public bool AutoInitialize = true;
		public bool LogActivityChanges;
		public bool SetLoggerParams;
		
		public float CurrentTransitionDuration => Time.unscaledTime - m_sequenceStartTime;
		
		private TreeActivity[] m_activities;
		
		private Sequencer m_sequencer;
		private TreeState m_targetState;

		private bool m_initialized;

		private float m_sequenceStartTime;

		private TreeState m_pendingTransition;

		private readonly TkLogger m_logger = TkLogger.Get<TreeStateManager>();
		
		private void Awake()
		{
			m_activities = GetComponentsInChildren<TreeActivity>();
			TreeStateRegistry.Instance.Register(name, this);
		}
		
		private void Start()
		{
			if (AutoInitialize) {
				Initialize();	
			}
		}

		private void OnDestroy()
		{
			TreeStateRegistry.Instance.Remove(name);
		}

		public void Initialize()
		{
			m_initialized = true;
			
			foreach (var activity in m_activities) {
				activity.SetMonitor(this);
			}
			
			foreach (var treeState in GetComponentsInChildren<TreeState>()) {
				treeState.Initialize(this);
			}
			
			var states = new List<TreeState> { StateMachine };
			AddDefaultStates(StateMachine, states);
			
			StartSequence(null, states);
		}

		public bool HandleTransition(string transitionName)
		{
			if (!TreeStateUtils.TryGetStateForTransition(transitionName, Active, out var target)) {
				return false;
			}

			m_pendingTransition = target;
			
			return true;
		}
		
		public void ActivityModeChanged(TreeActivity treeActivity)
		{
			if (SetLoggerParams) {
				TkLogger.SetCommonField("_state", treeActivity.name);
				TkLogger.SetCommonField("_stateActivity", treeActivity.TypeName);
				TkLogger.SetCommonField("_stateMode", treeActivity.Mode);
			}

			if (LogActivityChanges) {
				m_logger.Info($"[{treeActivity.Name}] -> [{treeActivity.Mode}]");	
			}

			var evt = new TreeActivityModeChangedEvt {
				State = treeActivity.Name,
				ActivityType = treeActivity.TypeName,
				Mode = treeActivity.Mode,
				IsState = treeActivity is TreeState
			};
			
			TreeStateRegistry.Instance.ActivityModeChanged.Emit(evt);

#if UNITY_EDITOR
			UnityEditor.EditorApplication.RepaintHierarchyWindow();
#endif
		}

		public void CurrentActivitySet(TreeActivity treeActivity)
		{
			if (LogActivityChanges) {
				m_logger.Info($"Active State: [{treeActivity.Name}]");	
			}
			
			Active = treeActivity as TreeState;
		}

		public void TransitionTo(TreeState treeState)
		{
			var commonAncestor = TreeStateUtils.GetCommonAncestor(Active, treeState);
			var unloadStates = TreeStateUtils.GetStatesToParent(Active, commonAncestor);
			var loadStates = TreeStateUtils.GetStatesToParent(treeState, commonAncestor);
			loadStates.Reverse();

			var treeStateMachine = loadStates[loadStates.Count - 1] as TreeStateMachine;
			
			if (treeStateMachine != null) {
				AddDefaultStates(treeStateMachine, loadStates);	
			}
			
			StartSequence(unloadStates, loadStates);
		}

		private void AddDefaultStates(TreeStateMachine treeStateMachine, List<TreeState> states)
		{
			while (true) {
				states.Add(treeStateMachine.DefaultState);

				if (treeStateMachine.DefaultState is TreeStateMachine state) {
					treeStateMachine = state;
					continue;
				}

				break;
			}
		}

		private void Update()
		{
			if (m_initialized == false) {
				return;
			}

			// There might a sequence triggered when the current sequence is completed.
			// Store the current sequencer and only clear the sequencer if it's the same one we had before we did an Update. 
			var currentSequence = m_sequencer;
			
			if (currentSequence != null && m_sequencer.Update()) {
				if (m_sequencer == currentSequence) {
					m_sequencer = null;	
				}

				if (LogActivityChanges) {
					m_logger.Info($"Transition Completed in {CurrentTransitionDuration}");
				}
			}
			
			foreach (var activity in m_activities) {
				activity.ActivityUpdate();
			}

			if (m_pendingTransition != null) {
				var pendingTransition = m_pendingTransition;
				m_pendingTransition = null;
				
				TransitionTo(pendingTransition);
			}
		}

		private void StartSequence(List<TreeState> unloadStates, List<TreeState> loadStates)
		{
			m_targetState = loadStates[loadStates.Count - 1];
			
			if (LogActivityChanges) {
				var fullName = TreeStateUtils.CalculatePath(m_targetState);
				m_logger.Info($"Transition started to [{fullName}]");	
			}
			
			m_sequenceStartTime = Time.unscaledTime;

			if (unloadStates != null) {
				m_sequencer = new Sequencer(new List<ISequence> {
					new SequencedDeactivation<TreeState>(unloadStates, this),
					new SequencedActivation<TreeState>(loadStates, true, this)
				});
			} else {
				m_sequencer = new Sequencer(new List<ISequence> {
					new SequencedActivation<TreeState>(loadStates, true, this)
				});
			}
			
			m_sequencer.Begin();
		}
	}
}