using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tekly.Injectors.Utils
{
    public static class TypeUtils
    {
        private static Dictionary<Type, bool> s_injectableTypes = new Dictionary<Type, bool>();
        
        public static ConstructorInfo Constructor(this Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0) {
                return null;
            }

            return constructors[0];
        }

        public static bool IsInjectable(MonoBehaviour obj)
        {
            return IsInjectable((object)obj);
        }
        
        public static bool IsInjectable(object obj)
        {
            if (obj == null) {
                return false;
            }

            var type = obj.GetType();
            
            if (!s_injectableTypes.TryGetValue(type, out var isInjectable))
            {
                isInjectable = IsInjectable(type);
                s_injectableTypes[type] = isInjectable;
            }

            return isInjectable;
        }
        
        private static bool IsInjectable(Type type)
        {
            return Injector.GetInjectableFields(type).Count > 0 || Injector.GetInjectableMethods(type).Count > 0;
        }
    }
}