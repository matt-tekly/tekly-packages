// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tekly.Injectors
{
    /// <summary>
    /// Injection data and injector for a specific type.
    /// </summary>
    public class Injector
    {
        public readonly Type Type;
        
        private readonly List<InjectableField> m_fields = new List<InjectableField>();
        
        public Injector(Type type)
        {
            Type = type;
            
            foreach (var fieldInfo in Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                var attribute = fieldInfo.GetCustomAttribute<InjectAttribute>();

                if (attribute != null) {
                    m_fields.Add(new InjectableField(attribute, fieldInfo));
                }
            }
        }

        public void Inject(object instance, InjectorContainer container)
        {
            foreach (var injectableField in m_fields) {
                injectableField.Inject(instance, container);
            }
        }

        public void Clear(object instance)
        {
            foreach (var injectableField in m_fields) {
                injectableField.Clear(instance);
            }
        }
    }

    public class InjectableField
    {
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