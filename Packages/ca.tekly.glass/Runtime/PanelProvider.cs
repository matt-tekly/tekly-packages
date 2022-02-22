using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using Tekly.Content;
using UnityEngine;

namespace Tekly.Glass
{
    public class PanelProvider
    {
        private readonly PanelsProvider m_panelsProvider;
        public readonly string Id;
        public readonly PanelSource Source;
        
        public Panel Panel { get; private set; }
        
        private readonly Task<PanelProvider> m_task;
        private readonly IContentOperation<GameObject> m_operation;
        private ComponentPool<Panel> m_pool;

        public TaskAwaiter<PanelProvider> GetAwaiter()
        {
            return m_task.GetAwaiter();
        }
        
        public PanelProvider(Panel panel, PanelsProvider panelsProvider)
        {
            m_panelsProvider = panelsProvider;
            Panel = PrefabProtector.Protect(panel);
            Panel.gameObject.SetActive(false);
            
            Id = Panel.name;
            Source = PanelSource.Embedded;
            m_task = Task.FromResult(this);
            m_pool = new ComponentPool<Panel>(Panel, m_panelsProvider.PoolContainer);
        }

        public PanelProvider(string id, PanelsProvider panelsProvider, IContentProvider contentProvider)
        {
            Id = id;
            m_panelsProvider = panelsProvider;
            
            Source = PanelSource.Addressable;
            m_operation = contentProvider.LoadAssetAsync<GameObject>(Id);
            m_task = GetTask();
        }

        public async Task<Panel> Get()
        {
            await m_task;
            return m_pool.Spawn();
        }

        public void Return(Panel panel)
        {
            m_pool.Recycle(panel);
        }

        private async Task<PanelProvider> GetTask()
        {
            var gameObject = await m_operation.Task;
            Panel = PrefabProtector.Protect(gameObject.GetComponent<Panel>());
            Panel.gameObject.SetActive(false);
            
            m_pool = new ComponentPool<Panel>(Panel, m_panelsProvider.PoolContainer);
            
            return this;
        }
    }
}