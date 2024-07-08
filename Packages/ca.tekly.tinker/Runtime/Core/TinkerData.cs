using System.Collections.Generic;
using System.Linq;
using Tekly.Tinker.Assets;
using Tekly.Tinker.Routing;

namespace Tekly.Tinker.Core
{
	public class TinkerData
	{
		public string Url;
		public List<ClassRoutes> Routes;
		public Sidebar Sidebar;
		public List<string> Css = new List<string>();
		public TinkerHome Home;

		public TinkerData(string url, List<ITinkerRoutes> routes, Sidebar sidebar, TinkerAssetRoutes assetRoutes, TinkerHome home)
		{
			Home = home;
			Url = url;
			Routes = routes.OfType<ClassRoutes>().ToList();
			Sidebar = sidebar;

			var cssAssets = new List<TinkerAsset>();
			assetRoutes.GetAssets("css", cssAssets);
			Css.AddRange(cssAssets.Select(x => x.Url));
		}
	}
}