// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using System.Text;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public enum ReferenceType
    {
        Owner,
        Shared
    }
    
    public struct ModelReference
    {
        public readonly IModel Model;
        public readonly ReferenceType ReferenceType;
        public readonly string Key;
        public readonly int Hash;
        
        public ModelReference(IModel model, ReferenceType referenceType, string key, int hash)
        {
            Model = model;
            ReferenceType = referenceType;
            Key = key;
            Hash = hash;
        }
    }
    
    // TODO: Make ObjectModel sortable
    
    public class ObjectModel : ModelBase
    {
        public IReadOnlyList<ModelReference> Models => m_models;
        
        public ITriggerable<ObjectModel> Modified => m_modified;
        
        private readonly Triggerable<ObjectModel> m_modified = new Triggerable<ObjectModel>();

        private readonly List<ModelReference> m_models = new List<ModelReference>(8);

        public void Add(string name, IModel model, ReferenceType referenceType = ReferenceType.Owner)
        {
            m_models.Add(new ModelReference(model, referenceType, name, name.GetHashCode()));
            m_modified.Emit(this);
        }

        public void RemoveModel(string name)
        {
            for (var index = 0; index < m_models.Count; index++) {
                var modelReference = m_models[index];
                if (modelReference.Key == name) {
                    m_models.RemoveAt(index);
                }
            }
            
            m_modified.Emit(this);
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

        protected virtual bool TryGetModel(string modelKey, out IModel model)
        {
            for (var index = 0; index < m_models.Count; index++) {
                var modelReference = m_models[index];
                if (modelReference.Key == modelKey) {
                    model = modelReference.Model;
                    return true;
                }
            }
            
            model = null;
            return false;
        }

        protected override void OnDispose()
        {
            for (var index = 0; index < m_models.Count; index++) {
                var modelReference = m_models[index];
                if (modelReference.ReferenceType == ReferenceType.Owner) {
                    modelReference.Model.Dispose();    
                }
            }
        }

        public override void ToJson(StringBuilder stringBuilder)
        {
            stringBuilder.Append("{");
            
            for (var index = 0; index < m_models.Count; index++) {
                var modelReference = m_models[index];
                
                stringBuilder.Append($"\"{modelReference.Key}\":");
                modelReference.Model.ToJson(stringBuilder);
                
                if (index < m_models.Count - 1) {
                    stringBuilder.Append(",");    
                }
            }

            stringBuilder.Append("}");
        }
    }
}