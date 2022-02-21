using Tekly.Balance;
using Tekly.Common.Utils;
using TeklySample.Game.Items;
using UnityEngine;

namespace TeklySample.Game.Generators
{
    [CreateAssetMenu(menuName = "Game/Balance/Generator")]
    public class GeneratorBalance : BalanceObject
    {
        public ItemBalance Item;
        public float CompletionTime;

        public Quantity<ItemBalance> ItemGeneration;
        public Quantity<ItemBalance>[] Cost;
    }
}