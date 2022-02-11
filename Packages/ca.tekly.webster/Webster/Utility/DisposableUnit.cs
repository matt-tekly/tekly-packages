using System;

namespace Tekly.Webster.Utility
{
	public class DisposableUnit : IDisposable
	{
		public static DisposableUnit Instance = new DisposableUnit();
		public void Dispose() { }
	}
}