using Tekly.DevBoard.Nodes;

namespace Tekly.DevBoard.Views
{
	/// <summary>
	/// Something that shows pages of nodes: a panel, or a remote viewer. Each host has its own view state, like
	/// which foldouts are open and what's typed into a search.
	/// </summary>
	public interface IViewHost
	{
		/// <summary>
		/// Unique among hosts. View state is keyed by it.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// True when the host is a passive overlay. Nodes can be limited to one mode or the other.
		/// </summary>
		bool IsOverlay { get; }

		/// <summary>
		/// Shows another page, e.g. from a link. Called from inside clicks and ticks, so hosts should act on it
		/// afterwards rather than straight away.
		/// </summary>
		void Open(string path);
	}

	/// <summary>
	/// What views and renderers know about the host they're showing nodes in.
	/// </summary>
	public sealed class ViewContext
	{
		public IViewHost Host { get; }

		public bool IsOverlay => Host.IsOverlay;

		public ViewContext(IViewHost host)
		{
			Host = host;
		}

		/// <summary>
		/// The DevBoard.State key for a piece of a node's view state in this host, e.g. whether a foldout is open.
		/// Hosts keep their own view state, so the same node can be open in one and closed in another.
		/// </summary>
		public string StateKey(Node node, string local)
		{
			return $"{Host.Id}/{node.Path}/{local}";
		}

		/// <summary>
		/// Shows another page in this host, after the current click or tick has finished.
		/// </summary>
		public void Open(string path)
		{
			Host.Open(path);
		}
	}
}
