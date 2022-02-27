using System;
using Tekly.Balance;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds.BuyMultipliers;

namespace TeklySample.Game.Worlds
{
    [Serializable]
    public class GameWorldSave
    {
        public ItemInventorySave Inventory;
        public GeneratorsSave Generators;
        public BuyMultiplierSave BuyMultiplier;
    }
    
    public class GameWorld
    {
        public readonly GeneratorManager GeneratorManager;
        public readonly ItemInventory ItemInventory;
        public readonly BuyMultiplier BuyMultiplier;
        
        public GameWorld(BalanceManager balanceManager, WorldBalance worldBalance, GameWorldSave gameWorldSave)
        {
            ItemInventory = new ItemInventory(balanceManager, gameWorldSave?.Inventory);
            GeneratorManager = new GeneratorManager(worldBalance, ItemInventory, gameWorldSave?.Generators);
            BuyMultiplier = new BuyMultiplier(ItemInventory, gameWorldSave?.BuyMultiplier);
        }

        public void Update()
        {
            GeneratorManager.Update();
        }

        public GameWorldSave ToSave()
        {
            return new GameWorldSave {
                Inventory = ItemInventory.ToSave(),
                Generators = GeneratorManager.ToSave(),
                BuyMultiplier = BuyMultiplier.ToSave()
            };
        }
    }
}