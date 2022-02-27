using Tekly.DataModels.Models;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds.BuyMultipliers;

namespace TeklySample.Game.Worlds
{
    public class GameWorldModel : ObjectModel
    {
        private readonly GameWorld m_gameWorld;
        private readonly ObjectModel m_generators = new ObjectModel();
        private readonly BuyMultiplierModel m_buyMultiplier;

        public GameWorldModel(GameWorld gameWorld)
        {
            m_gameWorld = gameWorld;
            m_buyMultiplier = new BuyMultiplierModel(m_gameWorld.BuyMultiplier);
            
            Add("generators", m_generators);
            Add("buymultiplier", m_buyMultiplier);
            Add("inventory", new ItemInventoryModel(gameWorld.ItemInventory));
            
            for (var index = 0; index < gameWorld.GeneratorManager.Generators.Count; index++) {
                var generator = gameWorld.GeneratorManager.Generators[index];
                
                var generatorModel = new GeneratorModel(generator, gameWorld.GeneratorManager, gameWorld.BuyMultiplier);
                m_generators.Add(generator.Balance.Id, generatorModel);
            }
        }
    }
}