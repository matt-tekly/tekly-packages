using System;

namespace Tekly.Common.LocalPrefs
{
    [Serializable]
    public class NamedValue<T>
    {
        public string Name;
        public T Value;
    }
    
    [Serializable]
    public class NamedString : NamedValue<string> {}
    
    [Serializable]
    public class NamedFloat : NamedValue<float> {}
    
    [Serializable]
    public class NamedBool : NamedValue<bool> {}
}