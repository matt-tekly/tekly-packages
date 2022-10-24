using System;
using System.Collections.Generic;

namespace Tekly.Common.LocalPrefs
{
    [Serializable]
    public class StringContainer : ValueContainer<NamedString, string> { }
    
    [Serializable]
    public class FloatContainer : ValueContainer<NamedFloat, float> { }
    
    [Serializable]
    public class BoolContainer : ValueContainer<NamedBool, bool> { }
    
    [Serializable]
    public class ValueContainer<TNamedValue, TV> where TV : IEquatable<TV> where TNamedValue : NamedValue<TV>, new()
    {
        public List<TNamedValue> Values = new List<TNamedValue>();

        public bool Set(string name, TV value)
        {
            var namedValue = Get(name);

            var isChanged = !EqualityComparer<TV>.Default.Equals(namedValue.Value, value);
            namedValue.Value = value;

            return isChanged;
        }

        public bool TryGet(string name, out TNamedValue outValue)
        {
            foreach (var value in Values) {
                if (value.Name == name) {
                    outValue = value;
                    return true;
                }
            }

            outValue = null;
            return false;
        }

        public void Remove(string name)
        {
            for (var index = 0; index < Values.Count; index++) {
                var value = Values[index];
                if (value.Name == name) {
                    Values.RemoveAt(index);
                    return;
                }
            }
        }
        
        private TNamedValue Get(string name)
        {
            if (TryGet(name, out var outValue)) {
                return outValue;
            }

            outValue = new TNamedValue();
            outValue.Name = name;
            Values.Add(outValue);

            return outValue;
        }
    }
}