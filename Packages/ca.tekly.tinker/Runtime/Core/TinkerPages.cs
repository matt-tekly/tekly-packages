namespace Tekly.Tinker.Core
{
	[Route("")]
	public class TinkerPages
	{
		[Page("", "tinker_home", "page")]
		public void Home() { }
		
		[Page("/", "tinker_home", "page")]
		public void Home2() { }

		[Page("/tinker/routes", "tinker_routes")]
		public void Routes() { }
		
		[Page("/tinker/terminal", "terminal")]
		public void Terminal() { }
	}
}