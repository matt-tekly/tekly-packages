using Tekly.DataModels.Binders;
using Tekly.Injectors;
using Tekly.TreeState.StandardActivities;
using UnityEngine;

namespace TeklySample.Game.Worlds
{
    public class GameWorldShimActivity : InjectableActivity
    {
        public GameObject Canvas;
        public Transform GeneratorContainer;
        public BinderContainer GeneratorTemplate;
        
        [Inject] private GameWorld m_gameWorld;

        protected override void Awake()
        {
            base.Awake();
            Canvas.SetActive(false);
        }
        
        protected override void ActiveStarted()
        {
            Canvas.SetActive(true);
            GeneratorTemplate.gameObject.SetActive(false);
            
            foreach (var generator in m_gameWorld.GeneratorManager.Generators) {
                var instance = Instantiate(GeneratorTemplate, GeneratorContainer);
                instance.Key.Path = $"gameworld.generators.{generator.Balance.Id}";
                
                instance.enabled = true;
                instance.gameObject.SetActive(true);
            }
        }
    }
}