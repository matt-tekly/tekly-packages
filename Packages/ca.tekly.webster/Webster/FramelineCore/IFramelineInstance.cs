using System;

namespace Tekly.Webster.FramelineCore
{
	public interface IFramelineInstance
	{
		string Json { get; }
		void Initialize();
		void InstantEvent(string id, string type);
		void BeginEvent(string id, string type);
		int BeginEventGetId(string id, string type);
		FramelineEventDisposable BeginEventDisposable(string id, string type);
		void EndEvent(int frameEventId);
		void EndEvent(string id, string type);
		void Clear();
		void ClearTo(double time);
		void ClearAfter(double time);
		void Update();
	}
}