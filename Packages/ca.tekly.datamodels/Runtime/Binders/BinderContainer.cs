// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class BinderContainer : Binder
    {
        public ModelRef Key;
        
        public bool BindOnEnable;

        public List<Binder> Binders;

        protected BinderContainer m_container;
        protected TkLogger m_logger => TkLogger.Get<BinderContainer>();
        
        [NonSerialized] protected string m_keyOverride = null;
        [NonSerialized] protected bool m_hasBound;
        
        public void OnEnable()
        {
            if (BindOnEnable) {
                Bind();
            }
        }

        public void OverrideKey(string key)
        {
            m_keyOverride = key;
            
            if (m_hasBound) {
                Bind();    
            }
        }

        public override void Bind(BinderContainer container)
        {
            m_container = container;
            Bind();
        }

        private string GetKey()
        {
            return m_keyOverride ?? Key.Path;
        }

        public virtual void Bind()
        {
            m_hasBound = true;

            if (string.IsNullOrEmpty(GetKey())) {
                return;
            }
            
            foreach (var binder in Binders) {
                if (binder == null) {
                    m_logger.ErrorContext("BinderContainer has null binder", this);
                    continue;
                }
                
                binder.Bind(this);
            }
        }

        public bool TryGet(string key, out IModel model)
        {
            var modelKey = ModelKey.Parse(key);
            return TryGet(modelKey, out model);
        }
        
        public bool TryGet(ModelKey modelKey, out IModel model)
        {
            var rootModel = ObjectModel.Instance;

            if (!modelKey.IsRelative) {
                return rootModel.TryGetModel(modelKey, 0, out model);
            }
            
            var selfKey = ModelKey.Parse(GetKey());

            if (selfKey.IsRelative) {
                if (m_container != null) {
                    m_container.TryGet(selfKey, out var targetRootModel);
                    rootModel = targetRootModel as ObjectModel;
                } else {
                    m_logger.ErrorContext("BinderContainer [{name}] has relative key but no container", this, ("name", gameObject.name));
                }
            } else {
                rootModel.TryGetModel(selfKey, 0, out var targetRootModel);
                rootModel = targetRootModel as ObjectModel;
            }
            
            return rootModel.TryGetModel(modelKey, 0, out model);
        }
        
        public bool TryGet<T>(string key, out T model) where T : class, IModel
        {
            var foundModel = TryGet(key, out var genericModel);

            if (foundModel) {
                if (genericModel is T specificModel) {
                    model = specificModel;
                } else {
                    model = null;
                    foundModel = false;
                    var fullPath = ResolveFullKey(key);
                    
                    m_logger.ErrorContext(
                        "Found model with key [{key} {fullPath}] but was wrong type. Expected [{expected}] actual [{actual}]",
                        this,
                        ("key", key),
                        ("fullPath", fullPath), 
                        ("expected", typeof(T)), 
                        ("actual", genericModel.GetType()));
                }
            } else {
                var fullPath = ResolveFullKey(key);
                m_logger.ErrorContext("Failed to find model [{key} {fullPath}]", this, ("key", key), ("fullPath", fullPath));
                model = null;
            }
            
            return foundModel;
        }
        
        public override string ResolveFullKey(string key)
        {
            if (key.Length == 1 && key[0] == '*') {
                return ResolveFullKey();
            }
            
            return ResolveFullKey() + "." +  ModelKey.StripRelativePrefix(key);
        }

        public string ResolveFullKey()
        {
            var selfKey = ModelKey.Parse(GetKey());

            if (selfKey == null) {
                return "";
            }
            
            var container = this.GetComponentInAncestor<BinderContainer>();
            
            if (selfKey.IsRelative && container == null) {
                Debug.LogError("Relative Key for BinderContainer with no parent", this);
                return GetKey();
            }
            
            if (selfKey.IsRelative) {
                var resolvedKey = container.ResolveFullKey(GetKey());
                return resolvedKey;
            }

            return GetKey();
        }
    }
}