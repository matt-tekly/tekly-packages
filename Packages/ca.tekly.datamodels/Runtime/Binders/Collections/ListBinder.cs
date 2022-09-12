using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders.Collections
{
    public class ListBinder : BinderContainer
    {
        [FormerlySerializedAs("Template")][SerializeField] private BinderContainer m_template;
        [FormerlySerializedAs("Container")][SerializeField] private RectTransform m_container;

        private IDisposable m_disposable;
        private List<BinderContainer> m_instances = new List<BinderContainer>();

        private BinderContainer m_protectedTemplate;

        private void Awake()
        {
            m_protectedTemplate = PrefabProtector.Protect(m_template);
        }

        public override void Bind()
        {
            if (TryGet(m_key.Path, out ObjectModel objectModel)) {
                m_disposable?.Dispose();
                m_disposable = objectModel.Modified.Subscribe(BindObjectModel);
                BindObjectModel(objectModel);
            } else {
                Clear();
            }

            base.Bind();
        }

        private void BindObjectModel(ObjectModel objectModel)
        {
            Clear();

            foreach (var modelReference in objectModel.Models) {
                var instance = Instantiate(m_protectedTemplate, m_container);
                instance.OverrideKey($"*.{modelReference.Key}");
                instance.gameObject.SetActive(true);

                m_instances.Add(instance);
                m_binders.Add(instance);
            }
        }

        private void Clear()
        {
            foreach (var instance in m_instances) {
                m_binders.Remove(instance);
                Destroy(instance.gameObject);
            }
            
            m_instances.Clear();
        }

        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}