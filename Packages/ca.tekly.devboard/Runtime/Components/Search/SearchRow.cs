namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// Passed to a search's row builder, once for each result shown. Rows are torn down and built again on every
	/// search, so read values through getters and undo anything you hook up with OnDispose.
	///
	/// OnDispose runs when the row is replaced by a new search, rebuilt, or when the search itself is destroyed
	/// (its page is rebuilt or removed, or its panel closes).
	/// </summary>
	public class SearchRow<T> : BuildContext
	{
		/// <summary>
		/// The result this row shows.
		/// </summary>
		public T Item { get; }

		/// <summary>
		/// The search text that found it, e.g. for highlighting what matched.
		/// </summary>
		public string Query { get; }

		/// <summary>
		/// The row's position in the results, from 0.
		/// </summary>
		public int Index { get; }

		private readonly SearchList<T> m_list;

		internal SearchRow(SearchList<T> list, ContainerWidget root, T item, string query, int index) : base(root)
		{
			m_list = list;
			Item = item;
			Query = query;
			Index = index;
		}

		internal bool IsBuilding { get; set; }

		/// <summary>
		/// Tears this row down and builds it again in place. Ignored while the row is being built.
		/// </summary>
		public void Rebuild()
		{
			m_list.RebuildRow(this);
		}
	}
}
