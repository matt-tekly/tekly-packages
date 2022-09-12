using System;
using System.Collections.Generic;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.DataModels.Binders.Collections
{
    public abstract class SortedListBinderEntryComparer : IComparer<SortedListBinderEntry>
    {
        public static readonly SortedListBinderEntryComparer Ascending = new AscendingImpl();
        public static readonly SortedListBinderEntryComparer Descending = new DescendingImpl();

        public static SortedListBinderEntryComparer GetComparer(SortOrder order)
        {
            return order == SortOrder.Ascending ? Ascending : Descending;
        }
        
        public abstract int Compare(SortedListBinderEntry x, SortedListBinderEntry y);

        private class AscendingImpl : SortedListBinderEntryComparer
        {
            public override int Compare(SortedListBinderEntry x, SortedListBinderEntry y)
            {
                return x.CompareTo(y);
            }
        }
        
        private class DescendingImpl : SortedListBinderEntryComparer
        {
            public override int Compare(SortedListBinderEntry x, SortedListBinderEntry y)
            {
                return y.CompareTo(x);
            }
        }
    }
    
    public class SortedListBinderEntry : IComparable<SortedListBinderEntry>, IValueObserver<Unit>, IDisposable
    {
        public readonly BinderContainer Instance;

        public string Key { get; private set; }

        private readonly SortedListBinder m_container;
        private readonly Transform m_transform;

        private ObjectModel m_model;
        private IValueModel m_sortValue;
        private IDisposable m_disposable;

        private readonly TkLogger m_logger = TkLogger.Get<SortedListBinderEntry>();

        public SortedListBinderEntry(SortedListBinder container, BinderContainer instance)
        {
            Instance = instance;
            m_container = container;
            m_transform = Instance.transform;
        }

        public void SetKey(string key, string sortKey)
        {
            Key = key;
            Instance.OverrideKey(key);

            if (m_container.TryGet(key, out m_model)) {
                SetSortKey(sortKey);
            } else {
                m_logger.ErrorContext("Failed to find key [{key}]", m_container, ("key", key));
            }
        }

        public void SetSortKey(string sortKey)
        {
            m_disposable?.Dispose();
            
            var sortModelKey = ModelKey.Parse(sortKey);

            if (m_model.TryGetModel(sortModelKey, 0, out var model)) {
                m_sortValue = model as IValueModel;
                if (m_sortValue != null) {
                    m_disposable = m_sortValue.Modified.Subscribe(this);    
                } else {
                    m_logger.ErrorContext("Found sort key [{key}] but was type [{type}] instead of IValueModel", m_container, ("key", sortKey), ("type", model.GetType().Name));    
                }
            } else {
                m_logger.ErrorContext("Failed to find sort key [{key}]", m_container, ("key", sortKey));
            }
        }

        public void SetIndex(int index)
        {
            m_transform.SetSiblingIndex(index);
        }

        public int CompareTo(SortedListBinderEntry other)
        {
            if (m_sortValue == null) {
                return -1;
            }
            
            return m_sortValue.CompareTo(other.m_sortValue);
        }

        public void Changed(Unit value)
        {
            m_container.Sort();
        }

        public void Dispose()
        {
            if (Instance && Instance.gameObject) {
                UnityEngine.Object.Destroy(Instance.gameObject);
            }

            m_disposable?.Dispose();
        }
    }
}