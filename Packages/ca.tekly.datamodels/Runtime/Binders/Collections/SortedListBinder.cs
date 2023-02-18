using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEngine;

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

        private IDisposable m_disposable;
        private List<SortedListBinderEntry> m_entries = new List<SortedListBinderEntry>();

        private BinderContainer m_templateProtected;

        private void Awake()
        {
            m_templateProtected = PrefabProtector.Protect(m_template);
        }

        public override void Bind()
        {
            if (string.IsNullOrEmpty(m_sortKey.Path)) {
                m_logger.ErrorContext("SortedListBinder has empty SortKey", this);    
            }
            
            if (TryGet(m_key.Path, out ObjectModel objectModel)) {
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

            var existingEntries = ListPool<SortedListBinderEntry>.Instance.Spawn();
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
            
            ListPool<SortedListBinderEntry>.Instance.Recycle(existingEntries);

            m_entries.Sort(SortedListBinderEntryComparer.GetComparer(Order));

            for (var index = 0; index < m_entries.Count; index++) {
                m_entries[index].SetIndex(index);
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

            var instance = Instantiate(m_templateProtected, m_container);
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