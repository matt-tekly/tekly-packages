using System;
using UnityEngine;

namespace Tekly.Common.LocalPrefs
{
    [Serializable]
    public class PrefsStore
    {
        [SerializeField] private FloatContainer m_floats = new FloatContainer();
        [SerializeField] private StringContainer m_strings = new StringContainer();
        [SerializeField] private BoolContainer m_bools = new BoolContainer();
        
        public bool Set(string name, float value)
        {
            return m_floats.Set(name, value);
        }
        
        public bool Set(string name, bool value)
        {
            return m_bools.Set(name, value);
        }
        
        public bool Set(string name, string value)
        {
            return m_strings.Set(name, value);
        }

        public float GetFloat(string name, float value = default)
        {
            if (m_floats.TryGet(name, out var namedValue)) {
                return namedValue.Value;
            }

            return value;
        }
        
        public bool GetBool(string name, bool value = default)
        {
            if (m_bools.TryGet(name, out var namedValue)) {
                return namedValue.Value;
            }

            return value;
        }
        
        public string GetString(string name, string value = default)
        {
            if (m_strings.TryGet(name, out var namedValue)) {
                return namedValue.Value;
            }

            return value;
        }
        
        public bool TryGetFloat(string name, out float value)
        {
            if (m_floats.TryGet(name, out var namedValue)) {
                value = namedValue.Value;
                return true;
            }

            value = default;
            return false;
        }
        
        public bool TryGetBool(string name, out bool value)
        {
            if (m_bools.TryGet(name, out var namedValue)) {
                value = namedValue.Value;
                return true;
            }

            value = default;
            return false;
        }
        
        public bool TryGetString(string name, out string value)
        {
            if (m_strings.TryGet(name, out var namedValue)) {
                value = namedValue.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}