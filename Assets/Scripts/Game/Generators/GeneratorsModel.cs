using Tekly.DataModels.Models;
using TeklySample.Game.Worlds;

namespace TeklySample.Game.Generators
{
    public class GeneratorsModel : ObjectModel
    {
        public GeneratorsModel(GameWorld gameWorld)
        {
            var all = new ObjectModel();
            Add("all", all);
            
            var visible = new ObjectModel();
            Add("visible", visible);
            
            foreach (var generator in gameWorld.GeneratorManager.Generators) {
                var generatorModel = new GeneratorModel(generator, gameWorld.GeneratorManager, gameWorld.BuyMultiplier);
                
                all.Add(generator.Balance.Id, generatorModel);
                    
                if (generator.Hidden) {
                    continue;
                }
                
                visible.Add(generator.Balance.Id, generatorModel, ReferenceType.Shared);
            }
        }
    }
}