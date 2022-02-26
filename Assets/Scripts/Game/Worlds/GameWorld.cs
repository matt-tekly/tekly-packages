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
        
        public GameWorld(BalanceManager balanceManager, WorldBalance worldBalance, GameWorldSave gameWorldSave)
        {
            ItemInventory = new ItemInventory(balanceManager, gameWorldSave?.Inventory);
            GeneratorManager = new GeneratorManager(worldBalance, ItemInventory, gameWorldSave?.Generators);
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