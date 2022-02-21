using System;
using Tekly.Balance;
using Tekly.Common.Utils;
using UnityEngine;

namespace TeklySample.Game.Items
{
    [CreateAssetMenu(menuName = "Game/Balance/Item")]
    public class ItemBalance : BalanceObject
    {
        public string NameId;
        public Sprite Icon;
    }
    
    [Serializable]
    public struct ItemCount : IQuantity<ItemBalance>
    {
        public ItemBalance Value;
        public double Count;

        public double GetCount() => Count;
        public ItemBalance GetValue() => Value;
    }
}