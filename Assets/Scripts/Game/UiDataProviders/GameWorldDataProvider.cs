using Tekly.DataModels.Models;
using Tekly.Injectors;
using TeklySample.Game.Worlds;
using UnityEngine;

namespace TeklySample.Game.UiDataProviders
{
    [CreateAssetMenu(menuName = "Game/Data Providers/Game World")]
    public class GameWorldDataProvider : UiDataProvider
    {
        [Inject] private GameWorld m_gameWorld;
        [Inject] private RootModel m_rootModel;

        private GameWorldModel m_gameWorldModel;
        
        public override void Bind()
        {
            m_gameWorldModel = new GameWorldModel(m_gameWorld);
            m_rootModel.Add("gameworld", m_gameWorldModel);
        }

        public override void Unbind()
        {
            m_rootModel.RemoveModel("gameworld");
            m_gameWorldModel.Dispose();
        }
    }
}