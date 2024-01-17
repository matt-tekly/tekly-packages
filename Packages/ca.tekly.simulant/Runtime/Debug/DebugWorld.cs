using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.Simulant.Core;

namespace Tekly.Simulant.Debug
{
	public class DebugWorlds : Singleton<DebugWorlds>
	{
		public List<DebugWorld> Worlds = new List<DebugWorld>();

		public void Add(World world, string name)
		{
			var debugWorld = new DebugWorld(world, name);
			Worlds.Add(debugWorld);
		}
	}
	
	public class DebugWorld : IWorldListener
	{
		public readonly string Name;
		public readonly World World;

		public DebugWorld(World world, string name)
		{
			Name = name;
			World = world;
			World.AddListener(this);
		}

		public void OnDestroyed(World world)
		{
			World.RemoveListener(this);
			DebugWorlds.Instance.Worlds.Remove(this);
		}
	}
}