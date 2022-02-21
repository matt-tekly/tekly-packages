using System;
using System.Collections.Generic;
using System.Linq;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds;
using UnityEngine;

namespace TeklySample.Game.Generators
{
    [Serializable]
    public class GeneratorsSave
    {
        public GeneratorSave[] Generators;
    }

    public class GeneratorManager
    {
        public readonly List<Generator> Generators = new List<Generator>();
        private readonly ItemInventory m_inventory;
        private readonly GeneratorSimulationResult m_simulationResult = new GeneratorSimulationResult();
        
        public GeneratorManager(WorldBalance worldBalance, ItemInventory inventory, GeneratorsSave save)
        {
            m_inventory = inventory;
            foreach (var worldGenerator in worldBalance.Generators) {
                var generator = CreateGenerator(worldGenerator, inventory, save?.Generators);
                Generators.Add(generator);
            }
        }

        private Generator CreateGenerator(WorldGenerator worldGenerator, ItemInventory inventory, GeneratorSave[] saves)
        {
            var item = inventory.Get(worldGenerator.Generator.Item.Id);

            var generatorSave = saves?.FirstOrDefault(x => x.GeneratorId == worldGenerator.Generator.Id);
            if (generatorSave != null) {
                return new Generator(worldGenerator.Generator, item, generatorSave);    
            }

            item.Count = worldGenerator.StartingCount;
            return new Generator(worldGenerator.Generator, item, worldGenerator.StartingMode);
        }

        public void Update()
        {
            ApplyTime(Time.deltaTime);
        }

        public void ApplyTime(float elapsedTime)
        {
            for (var index = Generators.Count - 1; index >= 0; index--) {
                var generator = Generators[index];
                m_simulationResult.Reset();
                
                generator.SimulateTime(elapsedTime, m_simulationResult);

                foreach (var generatedItem in m_simulationResult.GeneratedItems) {
                    var inventoryItem = m_inventory.Get(generatedItem.Value.Id);
                    inventoryItem.Count += generatedItem.Count;
                }
            }
        }

        public void Buy(Generator generator, double count)
        {
            m_inventory.Spend(generator.Cost, count);
            generator.InventoryItem.Count += count;
        }

        public GeneratorsSave ToSave()
        {
            return new GeneratorsSave {
                Generators = Generators.Select(x => x.ToSave()).ToArray()
            };
        }

        public double CalculateAffordableCount(Generator generator)
        {
            if (m_inventory.CanSpend(generator.Cost, 1)) {
                return 1;
            }

            return 0;
        }
    }
}