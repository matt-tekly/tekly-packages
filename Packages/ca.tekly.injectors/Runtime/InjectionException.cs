using System;
using System.Collections.Generic;
using System.Text;

namespace Tekly.Injectors
{
	/// <summary>
	/// Thrown when a dependency can't be resolved or an injected member fails. The message names the problem and
	/// then the chain of members that led to it, outermost last:
	/// <code>
	/// No instance of IGameService registered in container [Game]
	///   needed by Enemy.m_gameService
	///   needed by new Spawner(enemy)
	/// </code>
	/// Each field, method and constructor the exception passes through on the way out adds its own line, so a
	/// failure deep in a graph of factories says where it started.
	/// </summary>
	public class InjectionException : Exception
	{
		public override string Message {
			get {
				if (m_path.Count == 0) {
					return m_message;
				}

				var builder = new StringBuilder(m_message);

				foreach (var step in m_path) {
					builder.Append("\n  needed by ").Append(step);
				}

				return builder.ToString();
			}
		}

		/// <summary>
		/// The members the failure passed through, innermost first, e.g. "Enemy.m_gameService".
		/// </summary>
		public IReadOnlyList<string> Path => m_path;

		private readonly string m_message;
		private readonly List<string> m_path = new List<string>();

		public InjectionException(string message, Exception innerException = null) : base(message, innerException)
		{
			m_message = message;
		}

		/// <summary>
		/// Records that step was waiting on whatever failed. Returns this, to rethrow.
		/// </summary>
		public InjectionException NeededBy(string step)
		{
			m_path.Add(step);
			return this;
		}
	}
}
