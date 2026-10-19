namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Pages DevBoard brings itself, under "System". DevBoardInit registers them unless turned off there.
	/// </summary>
	public static class BuiltInPages
	{
		/// <summary>
		/// Adds the pages, removed again with DevBoard.Release(owner), or when owner is destroyed if it's a
		/// GameObject or component.
		/// </summary>
		public static void Register(object owner)
		{
			PerformancePage.Register(owner);
			TimePage.Register(owner);
			DisplayPage.Register(owner);
			DevicePage.Register(owner);
		}
	}
}
