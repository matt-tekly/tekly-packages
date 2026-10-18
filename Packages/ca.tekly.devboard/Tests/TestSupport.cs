using System;
using System.Reflection;
using Tekly.Common.Utils;
using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard
{
	/// <summary>
	/// What the core tests need that the runtime doesn't offer directly: a DevBoard instance outside play mode, a
	/// built scope to put nodes in, and a way to poll a list more than once in the same editor frame.
	/// </summary>
	internal static class TestSupport
	{
		/// <summary>
		/// Builds a scope with build and returns it, the way a segment is built the first time a panel shows it.
		/// </summary>
		public static ScopeNode BuildScope(Action<ScopeNode> build)
		{
			var scope = new ScopeNode(null, build);
			scope.Build();

			return scope;
		}

		/// <summary>
		/// Polls the list as a panel would on a new frame. Poll runs at most once per Time.frameCount, which doesn't
		/// move during an edit mode test, so the last polled frame is cleared first.
		/// </summary>
		public static void PollFresh(ListNode list)
		{
			var field = list.GetType().GetField("m_lastPollFrame", BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(list, -1);
			list.Poll();
		}

		/// <summary>
		/// Makes board the DevBoard.Instance for the test, since Singleton only creates one on entering play mode.
		/// Dispose puts it back.
		/// </summary>
		public static IDisposable UseBoard(DevBoard board)
		{
			var previous = DevBoard.Instance;
			SetInstance(board);

			return new Restore(() => SetInstance(previous));
		}

		private static void SetInstance(DevBoard board)
		{
			var property = typeof(Singleton<DevBoard>).GetProperty(nameof(Singleton<DevBoard>.Instance), BindingFlags.Public | BindingFlags.Static);
			property.GetSetMethod(true).Invoke(null, new object[] { board });
		}

		private sealed class Restore : IDisposable
		{
			private readonly Action m_restore;

			public Restore(Action restore)
			{
				m_restore = restore;
			}

			public void Dispose()
			{
				m_restore();
			}
		}
	}
}
