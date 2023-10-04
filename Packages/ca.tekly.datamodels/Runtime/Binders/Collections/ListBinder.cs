using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders.Collections
{
    public class ListBinder : BinderContainer
    {
        [SerializeField] private BinderContainer m_template;
        [SerializeField] private ItemTemplateProvider m_templateProvider;
        [SerializeField] private RectTransform m_container;

        private IDisposable m_disposable;
        private List<BinderContainer> m_instances = new List<BinderContainer>();
        
        public override void Bind()
        {
            if (TryGet(out var objectModel)) {
                m_disposable?.Dispose();
                m_disposable = objectModel.Modified.Subscribe(BindObjectModel);
                BindObjectModel(objectModel);
            } else {
                Clear();
            }
        }
        
        private bool TryGet(out ObjectModel objectModel)
        {
            var modelKey = ModelKey.Parse(GetKey());
            
            ObjectModel rootModel = RootModel.Instance;

            if (!modelKey.IsRelative) {
                var found = rootModel.TryGetModel(modelKey, 0, out objectModel);
                return found;
            }
            
            if (m_parent != null) {
                m_parent.TryGet(modelKey, out objectModel);
                return true;
            }
            
            m_logger.ErrorContext("ListBinder [{name}] has relative key but no container", this, ("name", gameObject.name));

            objectModel = null;
            return false;
        }

        private void BindObjectModel(ObjectModel objectModel)
        {
            Clear();

            if (m_templateProvider == null) {
                var template = PrefabProtector.Protect(m_template);
            
                foreach (var reference in objectModel.Models) {
                    CreateEntry(reference, template);
                }
            } else {
                foreach (var reference in objectModel.Models) {
                    var entryModel = reference.Model as ObjectModel;
                    var template = m_templateProvider.Get(entryModel);
                    CreateEntry(reference, template);
                }
            }
            
            base.Bind();
        }

        private void CreateEntry(ModelReference reference, BinderContainer template)
        {
            var instance = Instantiate(template, m_container);
            instance.OverrideKey($"*.{reference.Key}");
            instance.gameObject.SetActive(true);

            m_instances.Add(instance);
            m_binders.Add(instance);
        }

        private void Clear()
        {
            foreach (var instance in m_instances) {
                m_binders.Remove(instance);
                Destroy(instance.gameObject);
            }
            
            m_instances.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}