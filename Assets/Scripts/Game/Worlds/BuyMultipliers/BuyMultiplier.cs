using System;
using Tekly.Common.Maths;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;
using UnityEngine;

namespace TeklySample.Game.Worlds.BuyMultipliers
{
    public enum BuyMultiplierMode
    {
        One = 0,
        Percent10 = 1,
        Percent50 = 2,
        Percent100 = 3,
        ModeCount = 4,
    }

    [Serializable]
    public class BuyMultiplierSave
    {
        public BuyMultiplierMode Mode;
    }
    
    public class BuyMultiplier
    {
        private readonly ItemInventory m_inventory;
        public BuyMultiplierMode Mode { get; private set; }

        public BuyMultiplier(ItemInventory inventory, BuyMultiplierSave save)
        {
            m_inventory = inventory;
            
            if (save != null) {
                Mode = save.Mode;
            }
        }

        public void Toggle()
        {
            var intMode = ((int) Mode + 1) % (int) BuyMultiplierMode.ModeCount;
            Mode = (BuyMultiplierMode) intMode;
        }

        public BuyMultiplierSave ToSave()
        {
            return new BuyMultiplierSave {
                Mode = Mode
            };
        }
        
        public double CalculateAffordableCount(Generator generator)
        {
            var purchasableCount = m_inventory.PurchasableCount(generator.Cost);

            if (purchasableCount <= 0) {
                return 0;
            }
            
            if (MathUtils.IsApproximately(1d, purchasableCount)) {
                return 1;
            }
            
            switch (Mode) {
                case BuyMultiplierMode.One:
                    return purchasableCount >= 1 ? 1 : 0;
                case BuyMultiplierMode.Percent10:
                    return Math.Ceiling(purchasableCount * 0.1d);
                case BuyMultiplierMode.Percent50:
                    return Math.Ceiling(purchasableCount * 0.5d);
                case BuyMultiplierMode.Percent100:
                    return purchasableCount;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}