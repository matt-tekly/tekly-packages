using System.Collections.Generic;
using Tekly.Common.Collections;
using Tekly.Common.Utils;

namespace Tekly.DataModels.Models
{
    public class ModelManager : Singleton<ModelManager>
    {
        private readonly List<IModel> m_models = new List<IModel>(2048);
        private readonly LinkedList<ITickable> m_tickList = new LinkedList<ITickable>();
        
        public void AddModel(IModel model)
        {
            m_models.Add(model);
            
            if (model is ITickable tickable) {
                m_tickList.AddLast(tickable);
            }
        }

        public void RemoveModel(IModel model)
        {
            m_models.Remove(model);
            
            if (model is ITickable tickable) {
                m_tickList.Remove(tickable);
            }
        }
        
        public void Tick()
        {
            for (var node = m_tickList.First; node != null; node = node.Next) {
                node.Value.Tick();
            }
        }
    }
}