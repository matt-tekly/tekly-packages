using System;
using Tekly.Balance;
using TeklySample.Game.Generators;
using UnityEngine;

namespace TeklySample.Game.Worlds
{
    [CreateAssetMenu(menuName = "Game/Balance/World")]
    public class WorldBalance : BalanceObject
    {
        public WorldGenerator[] Generators;
    }

    [Serializable]
    public class WorldGenerator
    {
        public GeneratorBalance Generator;
        public int StartingCount;
        public GeneratorMode StartingMode = GeneratorMode.Manual;
    }
}