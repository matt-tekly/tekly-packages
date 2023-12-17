using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Tekly.Common.Utils.PropertyBags
{
    [Serializable]
    public abstract class Property
    {
        [SerializeField] private string m_id;
        
        public string Id => m_id;

        public PropertyBag Bag {
            get => m_propertyBag;
            set => m_propertyBag = value;
        }
        
        [NonSerialized] private PropertyBag m_propertyBag;

        protected Property(string id, PropertyBag bag)
        {
            m_id = id;
            Bag = bag;
        }

        protected virtual void Modified()
        {
            Bag.PropertyModified(this);
        }
        
        public abstract object AsObject();
    }

    [Serializable]
    public class ValueProperty<T> : Property
    {
        [SerializeField] private T m_v;

        public T Value {
            get => m_v;
            set {
                if (EqualityComparer<T>.Default.Equals(m_v, value)) {
                    return;
                }
                
                m_v = value;
                Modified();
            }
        }

        public ValueProperty(string id, PropertyBag bag) : base(id, bag) { }
        
        public override object AsObject()
        {
            return m_v;
        }

        public override string ToString()
        {
            return Id + ": " + m_v;
        }
    }

    [Serializable]
    public class NumberProperty : ValueProperty<double>
    {
        public NumberProperty(string id, PropertyBag bag) : base(id, bag) { }
    }
    
    [Serializable]
    public class StringProperty : ValueProperty<string>
    {
        public StringProperty(string id, PropertyBag bag) : base(id, bag) { }
    }
    
    [Serializable]
    public class BoolProperty : ValueProperty<bool>
    {
        public BoolProperty(string id, PropertyBag bag) : base(id, bag) { }
    }
    
    [Serializable]
    public class DateProperty : Property, ISerializationCallbackReceiver
    {
        [SerializeField] private string m_v;
        private DateTime m_dateTime;

        public DateTime Value {
            get => m_dateTime;
            set {
                if (EqualityComparer<DateTime>.Default.Equals(m_dateTime, value)) {
                    return;
                }
                
                m_dateTime = value;
                Modified();
            }
        }

        public DateProperty(string id, PropertyBag bag) : base(id, bag) { }
        
        public override object AsObject()
        {
            return m_v;
        }

        public override string ToString()
        {
            return Id + ": " + m_dateTime;
        }

        public void OnBeforeSerialize()
        {
            m_v = m_dateTime.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture);
        }

        public void OnAfterDeserialize()
        {
            DateTime.TryParse(m_v, out m_dateTime);
        }
    }
}