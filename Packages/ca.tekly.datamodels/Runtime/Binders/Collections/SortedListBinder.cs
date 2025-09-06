using System;
using System.Collections.Generic;
using Tekly.Common.Presentables;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.DataModels.Binders.Collections
{
    public enum SortOrder
    {
        Ascending,
        Descending
    }
    
    public class SortedListBinder : BinderContainer
    {
        public SortOrder Order {
            get => m_order;
            set {
                if (m_order != value) {
                    m_order = value;
                    Sort();
                }
            }
        }
        
        public string SortKey {
            get => m_sortKey.Path;
            set {
                if (m_sortKey.Path != value) {
                    m_sortKey.Path = value;
                    
                    foreach (var entry in m_entries) {
                        entry.SetSortKey(m_sortKey.Path);
                    }
                    
                    Sort();
                }
            }
        }
        
        [SerializeField] private ModelRef m_sortKey;
        [SerializeField] private SortOrder m_order;
        
        [SerializeField] private BinderContainer m_template;
        [SerializeField] private RectTransform m_container;
        [SerializeField] private GameObject m_emptyContent;

        private IDisposable m_disposable;
        private List<SortedListBinderEntry> m_entries = new List<SortedListBinderEntry>();

        private BinderContainer ProtectedTemplate {
            get {
                if (m_protectedTemplate == null) {
                    m_protectedTemplate = PrefabProtector.Protect(m_template);
                }
                return m_protectedTemplate;
            }
        }

        private BinderContainer m_protectedTemplate;

        private void Awake()
        {
            if (m_protectedTemplate == null) {
                m_protectedTemplate = PrefabProtector.Protect(m_template);
            }

            if (m_emptyContent != null) {
                m_emptyContent.SetActive(false);
            }
        }
        
        public override void Bind()
        {
            if (string.IsNullOrEmpty(m_sortKey.Path)) {
                m_logger.ErrorContext("SortedListBinder has empty SortKey", this);    
            }
            
            if (TryGet(ModelKey.RelativeKey, out ObjectModel objectModel)) {
                m_disposable?.Dispose();
                m_disposable = objectModel.Modified.Subscribe(BindObjectModel);
                BindObjectModel(objectModel);
            } else {
                Clear();
            }
        }

        [ContextMenu("Sort")]
        public void Sort()
        {
            m_entries.Sort(SortedListBinderEntryComparer.GetComparer(Order));
            
            for (var index = 0; index < m_entries.Count; index++) {
                m_entries[index].SetIndex(index);
            }
        }

        private void BindObjectModel(ObjectModel objectModel)
        {
            Clear();

            using (ListPool<SortedListBinderEntry>.Get(out var existingEntries))
            {
                existingEntries.AddRange(m_entries);
            
                m_entries.Clear();
            
                foreach (var modelReference in objectModel.Models) {
                    var entry = Get(modelReference.Key, existingEntries);
                    entry.SetKey($"*.{modelReference.Key}", m_sortKey.Path);

                    m_entries.Add(entry);
                    m_binders.Add(entry.Instance);
                }

                foreach (var entry in existingEntries) {
                    entry.Dispose();
                }
            }
     
            m_entries.Sort(SortedListBinderEntryComparer.GetComparer(Order));

            for (var index = 0; index < m_entries.Count; index++) {
                m_entries[index].SetIndex(index);
            }

            if (m_emptyContent != null) {
                Presentable.SetGameObjectActive(m_emptyContent, objectModel.Models.Count == 0);    
            }
            
            for (var index = 0; index < m_entries.Count; index++) {
                var entry = m_entries[index];
                entry.Instance.gameObject.SetActive(true);
            }
            
            base.Bind();
        }

        private SortedListBinderEntry Get(string key, List<SortedListBinderEntry> entries)
        {
            for (var index = 0; index < entries.Count; index++) {
                var entry = entries[index];
                if (entry.Key == key) {
                    entries.RemoveAt(index);
                    return entry;
                }
            }

            var instance = Instantiate(ProtectedTemplate, m_container);
            return new SortedListBinderEntry(this, instance);
        }

        private void Clear()
        {
            foreach (var entry in m_entries) {
                m_binders.Remove(entry.Instance);
                Destroy(entry.Instance.gameObject);
            }

            m_entries.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();

            foreach (var entry in m_entries) {
                entry.Dispose();
            }
        }
    }
}