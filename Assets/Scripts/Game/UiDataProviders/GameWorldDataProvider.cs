using System;
using Tekly.DataModels.Models;
using Tekly.Extensions.DataProviders;
using Tekly.Injectors;
using TeklySample.Game.Worlds;
using UnityEngine;

namespace TeklySample.Game.UiDataProviders
{
    [Serializable]
    public class GameWorldDataProvider : IUiDataProvider
    {
        [SerializeField] private string m_key = "gameworld";
        
        [Inject] private GameWorld m_gameWorld;

        private GameWorldModel m_gameWorldModel;
        
        public void Bind()
        {
            m_gameWorldModel = new GameWorldModel(m_gameWorld);
            RootModel.Instance.Add(m_key, m_gameWorldModel);
        }

        public void Unbind()
        {
            RootModel.Instance.RemoveModel(m_key);
        }
    }
}