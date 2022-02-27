using System;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;

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
            switch (Mode) {
                case BuyMultiplierMode.One:
                    if (m_inventory.CanSpend(generator.Cost)) {
                        return 1;
                    }
                    
                    return 0;
                case BuyMultiplierMode.Percent10:
                    return Math.Floor(m_inventory.PurchasableCount(generator.Cost) * 0.1d);
                case BuyMultiplierMode.Percent50:
                    return Math.Floor(m_inventory.PurchasableCount(generator.Cost) * 0.5d);
                case BuyMultiplierMode.Percent100:
                    return m_inventory.PurchasableCount(generator.Cost);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}