// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

namespace Tekly.TreeState.Sequences
{
	public interface ISequence
	{
		void Begin();
		
		bool IsDone();

		bool Update();
	}
}