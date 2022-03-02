using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using TeklySample.Game.Items;
using UnityEngine;

namespace TeklySample.Game.Generators
{
    [Serializable]
    public class GeneratorSave
    {
        public string GeneratorId;
        public float RatioComplete;
        public GeneratorMode Mode;
        public GeneratorState State;
    }

    public enum GeneratorMode
    {
        Manual,
        Automated
    }
    
    public enum GeneratorState
    {
        Inactive,
        Unlocked,
        Idle,
        Running,
    }

    public class GeneratorSimulationResult
    {
        public readonly List<ItemCount> GeneratedItems = new List<ItemCount>(3);

        public void Reset()
        {
            GeneratedItems.Clear();
        }
    }
    
    public class Generator
    {
        public readonly GeneratorBalance Balance;
        
        public float RatioComplete { get; private set; }
        public float CompletionTime => Balance.CompletionTime;
        public GeneratorMode Mode { get; set; }
        public GeneratorState State { get; private set; }
        public double TimeRemaining => (1f - RatioComplete) * CompletionTime;

        public bool Hidden => Balance.Hidden;

        public double Count => InventoryItem.Count;
        public ItemBalance GenerationItem => Balance.Item;
        public double GenerationCount => Balance.ItemGeneration.Count * Count;
        
        public readonly InventoryItem InventoryItem;

        public readonly List<Quantity<ItemBalance>> Cost = new List<Quantity<ItemBalance>>(3);

        public Generator(GeneratorBalance balance, InventoryItem inventoryItem, GeneratorSave save)
        {
            Balance = balance;
            InventoryItem = inventoryItem;
            
            Mode = save.Mode;
            State = save.State;
            RatioComplete = save.RatioComplete;

            Cost.AddRange(balance.Cost);
        }

        public Generator(GeneratorBalance balance, InventoryItem inventoryItem, GeneratorMode mode)
        {
            Balance = balance;
            InventoryItem = inventoryItem;
            
            Mode = mode;
            State = mode == GeneratorMode.Automated ? GeneratorState.Running : GeneratorState.Idle;

            Cost.AddRange(balance.Cost);
        }

        public void Run()
        {
            State = GeneratorState.Running;
        }

        public void SimulateTime(float elapsedTime, GeneratorSimulationResult result)
        {
            if (State != GeneratorState.Running) {
                return;
            }
            
            var timeComplete = CompletionTime * RatioComplete;
            timeComplete += elapsedTime;

            var dividend = timeComplete / CompletionTime;
            var completions = Mathf.FloorToInt(dividend);
            var remainingTime = dividend - completions;

            RatioComplete = remainingTime;

            if (completions == 0) {
                return;
            }
            
            if (Mode == GeneratorMode.Manual) {
                RatioComplete = 0;
                State = GeneratorState.Idle;    
            }
                
            result.GeneratedItems.Add(new ItemCount {
                Value = Balance.ItemGeneration.Value,
                Count = GenerationCount * completions
            });
        }

        public GeneratorSave ToSave()
        {
            return new GeneratorSave {
                RatioComplete = RatioComplete,
                GeneratorId = Balance.Id,
                State = State,
                Mode = Mode
            };
        }
    }
}