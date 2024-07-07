namespace Tekly.Tinker.Core
{
	public static class TinkerElements
	{
		public static HtmlContent StatsTopic(string topic)
		{
			return $"<stats-card topic=\"{topic}\" class=\"card box-shadow\"></stats-card>";
		}
	}
}