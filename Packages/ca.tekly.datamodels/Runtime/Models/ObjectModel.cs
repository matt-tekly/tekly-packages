// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tekly.DataModels.Models
{
    public class ObjectModel : IModel
    {
        public static ObjectModel Instance;
        
        private readonly Dictionary<string, IModel> m_models = new Dictionary<string, IModel>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, IModel>.Enumerator Models => m_models.GetEnumerator();
        
        public void Add(string name, IModel model)
        {
            m_models.Add(name, model);
        }

        public void RemoveModel(string name)
        {
            m_models.Remove(name);
        }

        public bool TryGetModel(ModelKey modelKey, int index, out IModel model)
        {
            if (modelKey.Keys.Length == 0) {
                model = this;
                return true;
            }
            
            var currentKey = modelKey.Keys[index];

            if (!TryGetModel(currentKey, out model)) {
                return false;
            }

            if (index == modelKey.Keys.Length - 1) {
                return true;
            }

            if (model is ObjectModel objectModel) {
                return objectModel.TryGetModel(modelKey, index + 1, out model);
            }
                
            return false;
        }

        public virtual bool TryGetModel(string modelKey, out IModel model)
        {
            return m_models.TryGetValue(modelKey, out model);
        }

        public void Dispose()
        {
            OnDispose();
            
            foreach (var model in m_models.Values) {
                model.Dispose();
            }
        }

        public void Tick()
        {
            OnTick();
            
            foreach (var value in m_models.Values) {
                value.Tick();
            }
        }
        
        protected virtual void OnTick()
        {
            
        }

        protected virtual void OnDispose()
        {
            
        }

        public virtual void ToJson(StringBuilder stringBuilder)
        {
            stringBuilder.Append("{");
            
            var keys = m_models.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++) {
                var model = m_models[keys[i]];
                stringBuilder.Append($"\"{keys[i]}\":");
                model.ToJson(stringBuilder);
                
                if (i < keys.Length - 1) {
                    stringBuilder.Append(",");    
                }
            }

            stringBuilder.Append("}");
        }
    }
}