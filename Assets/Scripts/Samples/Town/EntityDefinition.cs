using System;
using Tekly.Simulant.Core;

namespace TeklySample.Samples.Town
{
	public struct EntityInfo
	{
		public unsafe fixed char Name[32];
	}
	
	[Serializable]
	public abstract class EntityDefinition
	{
		public string Name;

		public void Construct(World world)
		{
			var entity = world.Create();
			var info = world.Add<EntityInfo>(entity);

			unsafe {
				Copy(Name, info.Name);
			}

			Construct(world, entity);
		}

		protected abstract void Construct(World world, int entity);
		
		private unsafe void Copy(string s, char* chars)
		{
			for (int i = 0, length = s.Length; i < length; i++) {
				chars[i] = s[i];
			}
		}
	}
}