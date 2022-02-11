// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using UnityEngine;

namespace Tekly.TreeState
{
	/// <summary>
	/// Derives from TreeState and exhibits all the same behaviour TreeState has. The major difference is that it cannot
	/// be a leaf state.
	///
	/// TreeStateMachines have a DefaultState that must be set. When a transition targets a TreeStateMachine the
	/// DefaultState will be added to the transition. This happens recursively until the transition ends on a TreeState.
	/// </summary>
	public class TreeStateMachine : TreeState
	{
		public TreeState DefaultState;
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (DefaultState == null) {
				Debug.LogError($"TreeStateMachine [{name}] doesn't have default state", gameObject);
			}
		}
#endif
	}
}