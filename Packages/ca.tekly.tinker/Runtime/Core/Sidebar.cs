using System.Collections.Generic;

namespace Tekly.Tinker.Core
{
	public class Sidebar
	{
		public SidebarItem Title = new SidebarItem { Name = "Tinker", Url = "/" };
		public List<SidebarSection> Sections = new List<SidebarSection>();

		public SidebarSection Section(string name)
		{
			var section = new SidebarSection() { Name = name };
			Sections.Add(section);
			return section;
		}
	}
	
	public class SidebarSection
	{
		public string Name;
		public List<SidebarItem> Items = new List<SidebarItem>();

		public SidebarSection Item(string name, string url)
		{
			Items.Add(new SidebarItem { Name = name, Url = url });
			return this;
		}
	}

	public class SidebarItem
	{
		public string Name;
		public string Url;
	}
}