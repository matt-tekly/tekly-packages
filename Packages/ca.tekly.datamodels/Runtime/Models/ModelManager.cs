using System.Collections.Generic;
using Tekly.Common.Collections;
using Tekly.Common.Utils;

namespace Tekly.DataModels.Models
{
    public class ModelManager : Singleton<ModelManager>
    {
        private readonly List<IModel> m_models = new List<IModel>(2056);
        private readonly SafeList<ITickable> m_tickList = new SafeList<ITickable>(2056);
        
        public void AddModel(IModel model)
        {
            m_models.Add(model);
            
            if (model is ITickable tickable) {
                m_tickList.Add(tickable);
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
            foreach (var model in m_tickList) {
                model.Tick();
            }
        }
    }
}