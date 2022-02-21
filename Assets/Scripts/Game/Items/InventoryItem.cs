using System;
using UnityEngine;

namespace TeklySample.Game.Items
{
    [Serializable]
    public class InventoryItemSave
    {
        public string ItemId;
        public double Count;
    }
    
    public class InventoryItem
    {
        public string ItemId => m_itemBalance.Id;
        public Sprite Icon => m_itemBalance.Icon;
        public string NameId => m_itemBalance.NameId;

        public double Count;
        
        private readonly ItemBalance m_itemBalance;

        public InventoryItem(ItemBalance itemBalance, double count)
        {
            m_itemBalance = itemBalance;
            Count = count;
        }

        public InventoryItemSave ToSave()
        {
            return new InventoryItemSave {
                ItemId = ItemId,
                Count = Count
            };
        }
    }
}