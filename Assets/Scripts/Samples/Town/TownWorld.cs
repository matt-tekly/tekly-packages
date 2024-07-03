using System;
using System.Collections.Generic;
using Tekly.Simulant.Core;

namespace TeklySample.Samples.Town
{
	public abstract class TownSystem
	{
		protected readonly World m_world;
		
		public TownSystem(World world)
		{
			m_world = world;
		}

		public abstract void Tick(float deltaTime);
	}
	
	public class TownWorld
	{
		public readonly World World;

		private readonly List<TownSystem> m_systems = new List<TownSystem>();

		public TownWorld()
		{
			World = new World();
			m_systems.Add(new GeneratorSystem(World));
			m_systems.Add(new InventorySystem(World));
		}

		public void Create(EntityDefinition definition)
		{
			definition.Construct(World);
		}

		public void Tick(float deltaTime)
		{
			foreach (var system in m_systems) {
				system.Tick(deltaTime);
			}
		}
	}

	[Serializable]
	public class WorldSave
	{
		public EntityData[] Entities;
	}
	
	[Serializable]
	public class DataSave<T>
	{
		public T[] Data;
		public EntityIndex[] Entities;
	}

	[Serializable]
	public struct EntityIndex
	{
		public int Entity;
		public int Index;
	}
}