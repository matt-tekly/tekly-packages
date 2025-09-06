using System;
using System.Collections.Generic;
using Tekly.Common.Presentables;
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
        [SerializeField] private GameObject m_emptyContent;
        
        private IDisposable m_disposable;
        private List<BinderContainer> m_instances = new List<BinderContainer>();

        private void Awake()
        {
            if (m_emptyContent != null) {
                m_emptyContent.SetActive(false);
            }
        }

        public override void Bind()
        {
            if (TryGet(ModelKey.RelativeKey, out ObjectModel objectModel)) {
                m_disposable?.Dispose();
                m_disposable = objectModel.Modified.Subscribe(BindObjectModel);
                BindObjectModel(objectModel);
            } else {
                Clear();
                m_logger.ErrorContext("ListBinder failed to find model for key [{key}", this, ("key", m_key));
            }
        }

        public override void UnBind()
        {
            m_disposable?.Dispose();
            base.UnBind();
            Clear();
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

            if (m_emptyContent != null) {
                Presentable.SetGameObjectActive(m_emptyContent, objectModel.Models.Count == 0);
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
    }
}