using System;
using Tekly.Webster.Utility;

namespace Tekly.Webster.FramelineCore
{
	public class FramelineInstanceDisabled : IFramelineInstance
	{
		public string Json { get; }
		public void Initialize()
		{
			throw new NotImplementedException();
		}

		public void InstantEvent(string id, string type)
		{
			throw new NotImplementedException();
		}

		public void BeginEvent(string id, string type)
		{
			throw new NotImplementedException();
		}

		public int BeginEventGetId(string id, string type)
		{
			return -1;
		}

		public IDisposable BeginEventDisposable(string id, string type)
		{
			return DisposableUnit.Instance;
		}

		public void EndEvent(int frameEventId)
		{
			throw new NotImplementedException();
		}

		public void EndEvent(string id, string type)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public void ClearTo(double time)
		{
			throw new NotImplementedException();
		}

		public void ClearAfter(double time)
		{
			throw new NotImplementedException();
		}

		public void Update()
		{
			throw new NotImplementedException();
		}
	}
}