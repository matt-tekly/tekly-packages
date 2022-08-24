using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class ListBinder : BinderContainer
    {
        public BinderContainer Template;
        public RectTransform Container;

        private IDisposable m_disposable;
        private List<BinderContainer> m_instances = new List<BinderContainer>();

        private BinderContainer m_template;

        private void Awake()
        {
            m_template = PrefabProtector.Protect(Template);
        }

        public override void Bind()
        {
            if (TryGet(Key.Path, out ObjectModel objectModel)) {
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
                var instance = Instantiate(m_template, Container);
                instance.Key.Path = $"*.{modelReference.Key}";
                instance.gameObject.SetActive(true);

                m_instances.Add(instance);
                Binders.Add(instance);
            }
        }

        private void Clear()
        {
            foreach (var instance in m_instances) {
                Binders.Remove(instance);
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