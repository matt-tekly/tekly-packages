using System.Collections.Generic;

namespace Tekly.Tinker.Core
{
	public class TinkerHome
	{
		public int CellHeight = 40;
		
		public List<TinkerHomeCard> Cards = new List<TinkerHomeCard>();
		
		public TinkerHome Add(string name, string url, int width, int height, bool isDefault = false, bool noResize = false)
		{
			var card = new TinkerHomeCard();
			card.Name = name;
			card.Url = url;
			card.Width = width;
			card.Height = height;
			card.Default = isDefault;
			card.NoResize = noResize;
			
			Cards.Add(card);
			
			return this;
		}
	}

	public class TinkerHomeCard
	{
		public string Url;
		public string Name;
		public int Width = 1;
		public int Height = 1;
		public bool Default;
		public bool NoResize;
	}
}