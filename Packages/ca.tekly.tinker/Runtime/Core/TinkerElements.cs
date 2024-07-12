using Tekly.Tinker.Http;

namespace Tekly.Tinker.Core
{
	public static class TinkerElements
	{
		public static HtmlContent StatsChannel(string channel)
		{
			return $"<stats-card channel=\"{channel}\" class=\"card box-shadow\"></stats-card>";
		}
	}
}