using Tekly.DataModels.Models;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds.BuyMultipliers;

namespace TeklySample.Game.Worlds
{
    public class GameWorldModel : ObjectModel
    {
        public GameWorldModel(GameWorld gameWorld)
        {
            Add("buymultiplier", new BuyMultiplierModel(gameWorld.BuyMultiplier));
            Add("inventory", new ItemInventoryModel(gameWorld.ItemInventory));
            
            Add("generators", new GeneratorsModel(gameWorld));
        }
    }
}
