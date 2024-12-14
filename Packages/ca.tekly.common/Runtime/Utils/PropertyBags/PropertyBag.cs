using System;
using System.Collections.Generic;
using Tekly.Common.Observables;
using UnityEngine;

namespace Tekly.Common.Utils.PropertyBags
{
    /// <summary>
    /// A serializable collection of properties
    /// </summary>
    [Serializable]
    public class PropertyBag : ISerializationCallbackReceiver
    {
        [SerializeField] private List<NumberProperty> m_numbers = new List<NumberProperty>();
        [SerializeField] private List<StringProperty> m_strings = new List<StringProperty>();
        [SerializeField] private List<BoolProperty> m_bools = new List<BoolProperty>();
        [SerializeField] private List<DateProperty> m_dates = new List<DateProperty>();

        /// <summary>
        /// Subscribe to this to know when any property is modified
        /// </summary>
        public ITriggerable<Property> Modified {
            get {
                if (m_modified == null) {
                    m_modified = new Triggerable<Property>();
                }

                return m_modified;
            }
        }
        
        private Triggerable<Property> m_modified;
        private long m_generation;

        public long Generation => m_generation;

        public void SetValue(string id, double value)
        {
            GetOrAdd(id, out NumberProperty target);
            target.Value = value;
        }
        
        public void SetValue(string id, string value)
        {
            GetOrAdd(id, out StringProperty target);
            target.Value = value;
        }
        
        public void SetValue(string id, bool value)
        {
            GetOrAdd(id, out BoolProperty target);
            target.Value = value;
        }
        
        public void SetValue(string id, DateTime value)
        {
            GetOrAdd(id, out DateProperty target);
            target.Value = value;
        }
        
        public double GetValue(string id, double defaultValue = default)
        {
            if (TryGet(id, out NumberProperty property)) {
                return property.Value;
            }

            return defaultValue;
        }
        
        public DateTime GetValue(string id, DateTime defaultValue = default)
        {
            if (TryGet(id, out DateProperty property)) {
                return property.Value;
            }

            return defaultValue;
        }
        
        public string GetValue(string id, string defaultValue = default)
        {
            if (TryGet(id, out StringProperty property)) {
                return property.Value;
            }

            return defaultValue;
        }
        
        public bool GetValue(string id, bool defaultValue = default)
        {
            if (TryGet(id, out BoolProperty property)) {
                return property.Value;
            }

            return defaultValue;
        }
        
        public void GetOrAdd(string id, out NumberProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new NumberProperty(id, this);
            m_numbers.Add(property);
        }
        
        public void GetOrAdd(string id, out StringProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new StringProperty(id, this);
            m_strings.Add(property);
        }

        public void GetOrAdd(string id, out BoolProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new BoolProperty(id, this);
            m_bools.Add(property);
        }
        
        public void GetOrAdd(string id, out DateProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new DateProperty(id, this);
            m_dates.Add(property);
        }
        
        public void GetOrAdd(string id, double defaultValue, out NumberProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new NumberProperty(id, this);
            property.Value = defaultValue;
            m_numbers.Add(property);
        }
        
        public void GetOrAdd(string id, string defaultValue, out StringProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new StringProperty(id, this);
            property.Value = defaultValue;
            m_strings.Add(property);
        }

        public void GetOrAdd(string id, bool defaultValue, out BoolProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new BoolProperty(id, this);
            property.Value = defaultValue;
            m_bools.Add(property);
        }
        
        public void GetOrAdd(string id, DateTime defaultValue, out DateProperty property)
        {
            if (TryGet(id, out property)) {
                return;
            }

            property = new DateProperty(id, this);
            property.Value = defaultValue;
            m_dates.Add(property);
        }
        
        public bool TryGet(string id, out NumberProperty property)
        {
            return m_numbers.TryGet(id, out property);
        }
        
        public bool TryGet(string id, out StringProperty property)
        {
            return m_strings.TryGet(id, out property);
        }
        
        public bool TryGet(string id, out BoolProperty property)
        {
            return m_bools.TryGet(id, out property);
        }
        
        public bool TryGet(string id, out DateProperty property)
        {
            return m_dates.TryGet(id, out property);
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            foreach (var property in m_numbers) {
                property.Bag = this;
            }
            
            foreach (var property in m_strings) {
                property.Bag = this;
            }
            
            foreach (var property in m_bools) {
                property.Bag = this;
            }
        }

        public void PropertyModified(Property property)
        {
            m_generation++;
            m_modified?.Emit(property);
        }
    }

    internal static class ListExt
    {
        public static bool TryGet<TProp>(this List<TProp> list, string id, out TProp target) where TProp : Property
        {
            for (var i = 0; i < list.Count; i++) {
                var property = list[i];
                if (property.Id != id) {
                    continue;
                }

                target = property;
                return true;
            }

            target = default;
            return false;
        }
    }
}