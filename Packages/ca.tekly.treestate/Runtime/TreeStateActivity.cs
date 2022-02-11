// ============================================================================
// Copyright 2021 Matt King
// ============================================================================
using Tekly.TreeState.Utils;

namespace Tekly.TreeState
{
	/// <summary>
	/// TreeStateActivity must be attached to a TreeState.
	///
	/// TreeStateActivity can be derived from to add additional behaviour to a state
	/// You can have many TreeStateActivitys attached to a single state.
	/// </summary>
	public class TreeStateActivity : TreeActivity
	{
		public override string Name => $"{name} [{GetType().Name}]";
		public override string FullName => m_fullName ??= TreeStateUtils.CalculatePath(TreeState);

		private string m_fullName;
		
		protected TreeState TreeState;
		
		protected virtual void Awake()
		{
			TreeState = GetComponent<TreeState>();
		}
	}
}