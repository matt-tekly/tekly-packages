using System;
using System.Collections.Generic;
using System.Reflection;
using Tekly.Injectors.Utils;

namespace Tekly.Injectors
{
    /// <summary>
    /// Injection data and injector for a specific type.
    /// </summary>
    public class Injector
    {
        private readonly Type m_type;
        private readonly List<InjectableField> m_fields;
        private readonly List<InjectableMethod> m_methods;
        private readonly InjectableConstructor m_constructor;

        private bool m_isInstantiating;

        private const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags METHOD_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        
        public Injector(Type type)
        {
            m_type = type;

            m_fields = GetInjectableFields(type);
            m_methods = GetInjectableMethods(type);

            m_constructor = new InjectableConstructor(type.Constructor());
        }

        public object Instantiate(InjectorContainer container)
        {
            // We need to detect cycles in instantiating objects. In the editor a stack overflow will just crash.
            if (m_isInstantiating) {
                throw new DependencyCycleException(m_type);
            }

            m_isInstantiating = true;
            var instance = m_constructor.Instantiate(container);
            Inject(instance, container);
            m_isInstantiating = false;
            
            return instance;
        }

        public void Inject(object instance, InjectorContainer container)
        {
            foreach (var injectableField in m_fields) {
                injectableField.Inject(instance, container);
            }
            
            foreach (var injectableMethod in m_methods) {
                injectableMethod.Inject(instance, container);
            }
        }

        public void Clear(object instance)
        {
            foreach (var injectableField in m_fields) {
                injectableField.Clear(instance);
            }
        }
        
        public static List<InjectableField> GetInjectableFields(Type type)
        {
            var injectableFields = new List<InjectableField>();
            
            do {
                var fields = type.GetFields(FIELD_BINDING_FLAGS);
                var length = fields.Length;
                
                for (var index = 0; index < length; index++) {
                    var fieldInfo = fields[index];
                    
                    var injectAttribute = fieldInfo.GetCustomAttribute<InjectAttribute>();
                    if (injectAttribute != null) {
                        injectableFields.Add(new InjectableField(injectAttribute, fieldInfo));
                    }
                }

                type = type.BaseType;
            } while (type != null);
            
            return injectableFields;
        }
        
        public static List<InjectableMethod> GetInjectableMethods(Type type)
        {
            var injectableMethods = new List<InjectableMethod>();
            
            do {
                var methods = type.GetMethods(METHOD_BINDING_FLAGS);
                var length = methods.Length;
                
                for (var index = 0; index < length; index++) {
                    var method = methods[index];

                    var injectAttribute = method.GetCustomAttribute<InjectAttribute>();
                    if (injectAttribute != null) {
                        injectableMethods.Add(new InjectableMethod(method));
                    }
                }
                
                type = type.BaseType;
            } while (type != null);
            
            return injectableMethods;
        }
    }

    public class InjectableConstructor
    {
        private readonly object[] m_parameterValues;
        private readonly ParameterInfo[] m_parameterInfos;
        private readonly Type m_type;
        
        public InjectableConstructor(ConstructorInfo constructorInfo)
        {
            m_type = constructorInfo.DeclaringType;
            
            m_parameterInfos = constructorInfo.GetParameters();
            if (m_parameterInfos.Length == 0) {
                m_parameterValues = Array.Empty<object>();
            } else {
                m_parameterValues = new object[m_parameterInfos.Length];
            }
        }

        public object Instantiate(InjectorContainer container)
        {
            for (var index = 0; index < m_parameterInfos.Length; index++) {
                var parameterInfo = m_parameterInfos[index];
                m_parameterValues[index] = container.Get(parameterInfo.ParameterType);
            }

            return Activator.CreateInstance(m_type, m_parameterValues);
        }
    }
    
    public class InjectableMethod
    {
        private readonly object[] m_parameterValues;
        private readonly ParameterInfo[] m_parameterInfos;
        private readonly MethodInfo m_methodInfo;
        
        public InjectableMethod(MethodInfo methodInfo)
        {
            m_methodInfo = methodInfo;

            m_parameterInfos = methodInfo.GetParameters();
            if (m_parameterInfos.Length == 0) {
                m_parameterValues = Array.Empty<object>();
            } else {
                m_parameterValues = new object[m_parameterInfos.Length];
            }
        }

        public void Inject(object instance, InjectorContainer container)
        {
            for (var index = 0; index < m_parameterInfos.Length; index++) {
                var parameterInfo = m_parameterInfos[index];
                m_parameterValues[index] = container.Get(parameterInfo.ParameterType);
            }

            m_methodInfo.Invoke(instance, m_parameterValues);
        }
    }

    public class InjectableField
    {
        public Type FieldType => m_fieldType;
        
        private readonly FieldInfo m_fieldInfo;
        private readonly Type m_fieldType;
        private readonly InjectAttribute m_injectAttribute;
        
        public InjectableField(InjectAttribute injectAttribute, FieldInfo fieldInfo)
        {
            m_injectAttribute = injectAttribute;
            m_fieldInfo = fieldInfo;
            m_fieldType = m_fieldInfo.FieldType;
        }
        
        public void Inject(object instance, InjectorContainer container)
        {
            if (m_injectAttribute.Id == null) {
                var data = container.Get(m_fieldType);
                m_fieldInfo.SetValue(instance, data);    
            } else {
                var data = container.Get(m_fieldType, m_injectAttribute.Id);
                m_fieldInfo.SetValue(instance, data);
            }
        }
        
        public void Clear(object instance)
        {
            m_fieldInfo.SetValue(instance, null);
        }
    }
}