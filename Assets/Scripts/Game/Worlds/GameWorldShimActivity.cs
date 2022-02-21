using Tekly.DataModels.Binders;
using Tekly.Glass;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.Game.Worlds
{
    public class GameWorldShimActivity : InjectableActivity
    {
        public Transform GeneratorContainer;
        public BinderContainer GeneratorTemplate;

        public string GeneratorPanel = "generator_panel";
        public string GameWorldPanel = "main_game_panel";
        public string GeneratorAttachment = "generators_att";

        public string Layer = "Game";

        [Inject] private GameWorld m_gameWorld;
        [Inject] private Glass m_glass;

        protected override void ActiveStarted()
        {
            LoadAsync();
        }

        private async void LoadAsync()
        {
            var gameWorldPanelTemplate = await m_glass.GetPanel(GameWorldPanel);
            var generatorPanelTemplate = await m_glass.GetPanel(GeneratorPanel);
            
            GeneratorTemplate = generatorPanelTemplate.Panel.GetComponent<BinderContainer>();

            var container = Instantiate(gameWorldPanelTemplate.Panel);
            GeneratorContainer = container.GetAttachment(GeneratorAttachment).transform;
            
            var layer = m_glass.GetLayer(Layer);
            layer.Add(container.gameObject);
            
            foreach (var generator in m_gameWorld.GeneratorManager.Generators) {
                var instance = Instantiate(GeneratorTemplate, GeneratorContainer);
                instance.Key.Path = $"gameworld.generators.{generator.Balance.Id}";
                instance.gameObject.SetActive(true);
            }

            container.gameObject.SetActive(true);
        }
    }
}