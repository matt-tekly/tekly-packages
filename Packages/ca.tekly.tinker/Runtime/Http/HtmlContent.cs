namespace Tekly.Tinker.Http
{
	public class HtmlContent
	{
		public string Content { get; set; }
		public HtmlContent(string content)
		{
			Content = content;
		}

		public override string ToString()
		{
			return Content;
		}

		public static implicit operator HtmlContent(string str) => new HtmlContent(str);
	}
}