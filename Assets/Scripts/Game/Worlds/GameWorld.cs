using System;
using Tekly.Balance;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;

namespace TeklySample.Game.Worlds
{
    [Serializable]
    public class GameWorldSave
    {
        public ItemInventorySave Inventory;
        public GeneratorsSave Generators;
    }
    
    public class GameWorld
    {
        public readonly GeneratorManager GeneratorManager;
        public readonly ItemInventory ItemInventory;
        
        public GameWorld(BalanceManager balanceManager, WorldBalance worldBalance)
        {
            ItemInventory = new ItemInventory(balanceManager, null);
            GeneratorManager = new GeneratorManager(worldBalance, ItemInventory, null);
        }

        public void Update()
        {
            GeneratorManager.Update();
        }

        public GameWorldSave ToSave()
        {
            return new GameWorldSave {
                Inventory = ItemInventory.ToSave(),
                Generators = GeneratorManager.ToSave()
            };
        }
    }
}