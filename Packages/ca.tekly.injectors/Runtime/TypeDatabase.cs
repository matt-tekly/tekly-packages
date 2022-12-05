using System;
using System.Collections.Generic;
using Tekly.Common.Utils;

namespace Tekly.Injectors
{
    public class TypeDatabase : Singleton<TypeDatabase>
    {
        private readonly Dictionary<Type, Injector> m_injectors = new Dictionary<Type, Injector>();
        
        public Injector GetInjector(Type type)
        {
            if (!m_injectors.TryGetValue(type, out var injectionTypeData)) {
                injectionTypeData = new Injector(type);
                m_injectors.Add(type, injectionTypeData);
            }

            return injectionTypeData;
        }
    }
}