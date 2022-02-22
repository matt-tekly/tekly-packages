using Tekly.DataModels.Models;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;

namespace TeklySample.Game.Worlds
{
    public class GameWorldModel : ObjectModel
    {
        private readonly GameWorld m_gameWorld;
        private readonly ObjectModel m_generators = new ObjectModel();

        public GameWorldModel(GameWorld gameWorld)
        {
            m_gameWorld = gameWorld;

            Add("generators", m_generators);
            Add("inventory", new ItemInventoryModel(gameWorld.ItemInventory));
            
            for (var index = 0; index < gameWorld.GeneratorManager.Generators.Count; index++) {
                var generator = gameWorld.GeneratorManager.Generators[index];
                
                var generatorModel = new GeneratorModel(generator, gameWorld.GeneratorManager);
                m_generators.Add(generator.Balance.Id, generatorModel);
            }
        }
    }
}