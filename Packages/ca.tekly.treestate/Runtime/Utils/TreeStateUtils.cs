// ============================================================================
// Copyright 2021 Matt King
// ============================================================================
using System;
using System.Collections.Generic;
using System.Text;
using Tekly.Logging;

namespace Tekly.TreeState.Utils
{
	public static class TreeStateUtils
	{
		private static readonly TkLogger s_logger = TkLogger.Get(typeof(TreeStateUtils));
		
		public static TreeState GetCommonAncestor(TreeState targetA, TreeState targetB)
		{
			var machine = targetA.Machine;
			while (machine != null) {
				if (IsAncestorOf(machine, targetB)) {
					return machine;
				}

				machine = machine.Machine;
			}

			throw new Exception("Failed to find common ancestor");
		}
		
		public static bool IsAncestorOf(TreeState target, TreeState child)
		{
			while (child.Machine != null) {
				if (child.Machine == target) {
					return true;
				}

				child = child.Machine;
			}

			return false;
		}

		public static List<TreeState> GetStatesToParent(TreeState start, TreeState parent)
		{
			var parents = new List<TreeState>();
			
			do {
				parents.Add(start);
				start = start.Machine;
			} while (start != parent && start != null);

			if (start != parent) {
				throw new Exception("Failed to GetStatesToParent");
			}
			
			return parents;
		}
		
		public static void GetValidTransitions(TreeState active, List<string> outTransitions)
		{
			var state = active;

			while (state != null) {
				foreach (var transition in state.Transitions) {
					if (!outTransitions.Contains(transition.TransitionName)) {
						outTransitions.Add(transition.TransitionName);	
					}
				}

				if (state.transform.parent == null) {
					return;
				}

				state = state.transform.parent.GetComponent<TreeState>();
			}
		}

		public static bool TryGetStateForTransition(string transitionName, TreeState start, out TreeState target)
		{
			do {
				if (start.TryGetTransition(transitionName, out target)) {
					return true;
				}

				start = start.Machine;
			} while (start != null);
			
			return false;
		}
		
		private static readonly List<TreeState> s_scratchList = new List<TreeState>(16);
		private static readonly StringBuilder s_builder = new StringBuilder();
		
		public static string CalculatePath(TreeState state)
		{
			s_builder.Clear();
			s_scratchList.Clear();
			
			while (true) {
				s_scratchList.Add(state);
				
				if (state.Machine == null) {
					break;
				}
				
				state = state.Machine;
			}
			
			s_scratchList.Reverse();

			s_builder.Append(s_scratchList[0].Name);

			for (var index = 1; index < s_scratchList.Count; index++) {
				s_builder.Append("/");
				s_builder.Append(s_scratchList[index].Name);
			}

			return s_builder.ToString();
		}
	}
}