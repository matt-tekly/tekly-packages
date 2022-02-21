using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using Tekly.Content;
using UnityEngine;

namespace Tekly.Glass
{
    public class LoadedPanel
    {
        public readonly string Id;
        public readonly PanelSource Source;
        
        public Panel Panel { get; private set; }

        private readonly Task<LoadedPanel> m_task;
        private readonly IContentOperation<GameObject> m_operation;
        
        public TaskAwaiter<LoadedPanel> GetAwaiter()
        {
            return m_task.GetAwaiter();
        }
        
        public LoadedPanel(Panel panel)
        {
            Panel = PrefabProtector.Protect(panel);
            Panel.gameObject.SetActive(false);
            
            Id = Panel.name;
            Source = PanelSource.Embedded;
            m_task = Task.FromResult(this);
        }

        public LoadedPanel(string id, IContentProvider contentProvider)
        {
            Id = id;
            Source = PanelSource.Addressable;
            m_operation = contentProvider.LoadAssetAsync<GameObject>(Id);
            m_task = GetTask();
        }

        private async Task<LoadedPanel> GetTask()
        {
            var gameObject = await m_operation.Task;
            Panel = PrefabProtector.Protect(gameObject.GetComponent<Panel>());
            Panel.gameObject.SetActive(false);
            
            return this;
        }
    }
}