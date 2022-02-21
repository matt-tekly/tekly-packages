using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Balance;

namespace TeklySample.Game.Items
{
    [Serializable]
    public class ItemInventorySave
    {
        public InventoryItemSave[] Items;
    }
    
    public class ItemInventory
    {
        private readonly BalanceManager m_balanceManager;
        private readonly Dictionary<string, InventoryItem> m_items = new();

        public ItemInventory(BalanceManager balanceManager, ItemInventorySave save)
        {
            m_balanceManager = balanceManager;
            
            if (save != null && save.Items != null) {
                foreach (var item in save.Items) {
                    var inventoryItem = CreateItem(item.ItemId, item.Count);
                    m_items.Add(inventoryItem.ItemId, inventoryItem);
                }
            }
        }
        
        public InventoryItem Get(string itemId)
        {
            if (!m_items.TryGetValue(itemId, out var item)) {
                item = CreateItem(itemId);
                
                m_items.Add(itemId, item);
            }

            return item;
        }

        public void ModifyCount(string itemName, double count)
        {
            var item = Get(itemName);
            item.Count += count;
        }

        public void Spend(List<ItemCount> items, double count = 1)
        {
            foreach (var itemCount in items) {
                ModifyCount(itemCount.Value.Id, -itemCount.Count * count);
            }
        }
        
        public bool CanSpend(List<ItemCount> items, double count = 1)
        {
            foreach (var itemCount in items) {
                var inventoryItem = Get(itemCount.Value.Id);
                
                if (inventoryItem.Count < itemCount.Count * count) {
                    return false;
                }
            }

            return true;
        }

        public void SetCount(string itemName, double count)
        {
            var item = Get(itemName);
            item.Count = count;
        }

        public ItemInventorySave ToSave()
        {
            return new ItemInventorySave {
                Items = m_items.Values.Select(x => x.ToSave()).ToArray()
            };
        }

        private InventoryItem CreateItem(string itemId, double amount = 0)
        {
            return new InventoryItem(m_balanceManager.Get<ItemBalance>(itemId), amount);
        }
    }

    
}