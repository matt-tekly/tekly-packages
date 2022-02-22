using System;
using Tekly.Glass;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.TreeState.StandardActivities;

namespace TeklySample.Game.Worlds
{
    public class PanelActivity : InjectableActivity
    {
        public string Panel = "main_game_panel";
        public string Layer = "Game";
        
        [Inject] private Glass m_glass;
        
        private readonly TkLogger m_logger = TkLogger.Get<PanelActivity>();
        private bool m_isLoading;

        private PanelProvider m_panelProvider;
        
        private Panel m_panelInstance;
        private Layer m_layer;
        
        protected override bool IsDoneLoading()
        {
            return !m_isLoading;
        }

        protected override void LoadingStarted()
        {
            m_isLoading = true;
            LoadAsync();
        }
        
        protected override void UnloadingStarted()
        {
            if (m_panelProvider != null) {
                m_panelProvider.Return(m_panelInstance);
                m_layer.Remove(m_panelProvider.Panel.gameObject);

                m_panelInstance = null;
            }
        }

        private async void LoadAsync()
        {
            try {
                m_panelProvider = m_glass.GetPanel(Panel);
                m_panelInstance = await m_panelProvider.Get();
                
                m_layer = m_glass.GetLayer(Layer);
                m_layer.Add(m_panelInstance.gameObject);

                m_panelInstance.gameObject.SetActive(true);
            }
            catch (Exception exception) {
                m_logger.ExceptionContext(exception, "Failed to Load PanelActivity", this);
            }

            m_isLoading = false;
        }
    }
}