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
            
            foreach (var generator in gameWorld.GeneratorManager.Generators) {
                m_generators.Add(generator.Balance.Id, new GeneratorModel(generator, gameWorld.GeneratorManager));
            }
        }
    }
}