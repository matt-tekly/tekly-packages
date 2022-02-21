using Tekly.Balance;
using TeklySample.Game.Items;
using UnityEngine;

namespace TeklySample.Game.Generators
{
    [CreateAssetMenu(menuName = "Game/Balance/Generator")]
    public class GeneratorBalance : BalanceObject
    {
        public ItemBalance Item;
        public float CompletionTime;

        public ItemCount ItemGeneration;
        public ItemCount[] Cost;
    }
}