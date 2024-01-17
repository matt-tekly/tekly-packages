using System.Collections.Generic;
using Tekly.Simulant.Core;

namespace TeklySample.Samples.Town
{
	public struct AddToInventory
	{
		public int Count;
		public string Resource;
	}

	public class InventoryItem
	{
		public string Item;
		public int Count;
	}

	public class InventorySystem : TownSystem
	{
		private readonly DataPool<AddToInventory> m_addPool;
		private readonly Query m_adds;

		private Dictionary<string, InventoryItem> m_items = new Dictionary<string, InventoryItem>();

		public InventorySystem(World world) : base(world)
		{
			m_addPool = world.GetPool<AddToInventory>();

			m_adds = world.Query<AddToInventory>();
		}
		
		public override void Tick(float deltaTime)
		{
			foreach (var entity in m_adds) {
				ref var data = ref m_addPool.Get(entity);

				if (!m_items.TryGetValue(data.Resource, out var item)) {
					item = new InventoryItem();
					item.Item = data.Resource;
					m_items[data.Resource] = item;
				}

				item.Count += data.Count;
				m_addPool.Delete(entity);
			}
		}
	}
}