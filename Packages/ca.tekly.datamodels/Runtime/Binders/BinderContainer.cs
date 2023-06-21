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
        [FormerlySerializedAs("Key")][SerializeField] protected ModelRef m_key;
        [FormerlySerializedAs("BindOnEnable")][SerializeField] protected bool m_bindOnEnable;
        [FormerlySerializedAs("Binders")][SerializeField] protected List<Binder> m_binders;

        [NonSerialized] protected BinderContainer m_parent;
        [NonSerialized] protected string m_keyOverride;
        [NonSerialized] protected bool m_hasBound;

        protected TkLogger m_logger => TkLogger.Get<BinderContainer>();
        
        public bool BindOnEnable => m_bindOnEnable;
        public List<Binder> Binders => m_binders;
        
        public void OnEnable()
        {
            if (m_bindOnEnable) {
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

        public override void Bind(BinderContainer parent)
        {
            m_parent = parent;
            Bind();
        }

        public override void UnBind()
        {
            if (!m_hasBound) {
                return;
            }

            m_hasBound = false;
            
            foreach (var binder in m_binders) {
                if (binder == null) {
                    m_logger.ErrorContext("BinderContainer has null binder", this);
                    continue;
                }
                
                binder.UnBind();
            }
        }

        protected string GetKey()
        {
            return m_keyOverride ?? m_key.Path;
        }

        public virtual void Bind()
        {
            m_hasBound = true;

            foreach (var binder in m_binders) {
                if (binder == null) {
                    m_logger.ErrorContext("BinderContainer has null binder", this);
                    continue;
                }
                
                binder.Bind(this);
            }
        }
        
        public bool TryGet<T>(string key, out T model) where T : class, IModel
        {
            var modelKey = ModelKey.Parse(key);
            return TryGet(modelKey, out model);
        }
        
        public bool TryGet<T>(ModelKey modelKey, out T model) where T : class, IModel
        {
            var foundModel = TryGet(modelKey, out var genericModel);

            if (foundModel) {
                if (genericModel is T specificModel) {
                    model = specificModel;
                } else {
                    model = null;
                    foundModel = false;
                    var key = modelKey.ToString();
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
                var key = modelKey.ToString();
                var fullPath = ResolveFullKey(key);
                m_logger.ErrorContext("Failed to find model [{key} {fullPath}]", this, ("key", key), ("fullPath", fullPath));
                model = null;
            }
            
            return foundModel;
        }

        private bool TryGet(string key, out IModel model)
        {
            var modelKey = ModelKey.Parse(key);
            return TryGet(modelKey, out model);
        }

        private bool TryGet(ModelKey modelKey, out IModel model)
        {
            ObjectModel rootModel = RootModel.Instance;

            if (!modelKey.IsRelative) {
                return rootModel.TryGetModel(modelKey, 0, out model);
            }
            
            var selfKey = ModelKey.Parse(GetKey());

            if (selfKey.IsRelative) {
                if (m_parent != null) {
                    m_parent.TryGet(selfKey, out var targetRootModel);
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
                if (BindOnEnable) {
                    m_logger.ErrorContext("BinderContainer [{name}] has relative key with BindOnEnable and no parent container", this, ("name", gameObject.name));
                }
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